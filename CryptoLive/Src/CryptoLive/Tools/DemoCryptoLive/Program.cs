using System;
using System.Threading.Tasks;
using Common;
using CryptoLive;
using Infra;
using Microsoft.Extensions.Logging;
using Utils;
using Utils.Abstractions;

namespace DemoCryptoLive
{
    public class Program
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<Program>();
        private static readonly string s_configFile = "appsettings.json";

        public static int Main()
        {
            try
            {
                CryptoDemoParameters appParameters = AppParametersLoader<CryptoDemoParameters>.Load(s_configFile);
                s_logger.LogInformation(appParameters.ToString());
                RunMultiplePhases(appParameters).Wait();

                return 0;
            }
            catch(Exception e)
            {
                s_logger.LogError(e.Message);
                return 1;
            }
        }

        private static async Task RunMultiplePhases(CryptoDemoParameters appParameters)
        {
            int totalWinCounter = 0;
            int totalLossCounter = 0;
            int totalEvenCounter = 0;
            ICryptoBotPhasesFactory cryptoBotPhasesFactory = CreateCryptoBotPhasesGenerator(appParameters);
            foreach (string currency in appParameters.Currencies)
            {
                CryptoBot cryptoBot = CreateCryptoBot(appParameters, cryptoBotPhasesFactory, currency);
                (int winCounter, int lossCounter, int evenCounter) = await RunMultiplePhasesPerCurrency(cryptoBot);
                totalWinCounter += winCounter;
                totalLossCounter += lossCounter;
                totalEvenCounter += evenCounter;
            }
            
            s_logger.LogInformation($"Total Summary:  Win - {totalWinCounter}, Loss - {totalLossCounter}, Even: {totalEvenCounter}");
        }

        private static CryptoBot CreateCryptoBot(CryptoDemoParameters appParameters, ICryptoBotPhasesFactory cryptoBotPhasesFactory, string currency) =>
            new CryptoBot(cryptoBotPhasesFactory, currency, 
                appParameters.MaxRsiToNotify, 
                appParameters.CandleSize, 
                appParameters.CandleSize, 
                appParameters.CandleSize, 
                300, 
                appParameters.PriceChangeToNotify, 
                5,
                appParameters.RsiCandlesAmount);

        private static ICryptoBotPhasesFactory CreateCryptoBotPhasesGenerator(CryptoDemoParameters appParameters)
        {
            ISystemClock systemClock = new DummySystemClock();
            INotificationService notificationService =
                NotificationServiceFactory.CreateNotificationService(NotificationType.Disable);
            ICurrencyService currencyService = new DemoCurrencyService(appParameters.CandlesDataFolder, appParameters.Currencies);
            return new CryptoBotPhasesFactory(notificationService, currencyService, systemClock);
        }

        private static async Task<(int winCounter, int lossCounter, int evenCounter)> RunMultiplePhasesPerCurrency(CryptoBot cryptoBot)
        {
            int winCounter = 0;
            int lossCounter = 0;
            int evenCounter = 0;
            DateTime initialTime = DateTime.Parse("1/16/2021  2:12:00 AM");
            DateTime currentTime = initialTime;
            bool gotException = false;
            while(!gotException)
            {
                try
                {
                    BotResult botResult;
                    (botResult, currentTime) = await cryptoBot.StartAsync(currentTime);
                    switch (botResult)
                    {
                        case BotResult.Gain:
                            winCounter++;
                            break;
                        case BotResult.Even:
                            evenCounter++;
                            break;
                        case BotResult.Loss:
                            lossCounter++;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                catch (Exception e)
                {
                    s_logger.LogError(e.Message);
                    gotException = true;
                }
            }

            s_logger.LogInformation($"{cryptoBot.Currency} Summary:  Win - {winCounter}, Loss - {lossCounter}, Even: {evenCounter}");
            return (winCounter, lossCounter, evenCounter);
        }
    }
}
