using System;
using System.Threading.Tasks;
using Common;
using Infra;
using Microsoft.Extensions.Logging;
using Utils;
using Utils.Abstractions;
using Utils.CurrencyService;

namespace CryptoLive
{
    public class Program
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<Program>();
        private static readonly string s_configFile = "appsettings.json";
        
        public static int Main()
        {
            try
            {
                CryptoLiveParameters cryptoLiveParameters = AppParametersLoader<CryptoLiveParameters>.Load(s_configFile);
                if (cryptoLiveParameters.AppMode.Equals(AppMode.FullMode))
                {
                    RunMultiplePhases(cryptoLiveParameters).Wait();
                }

                return 0;
            }
            catch(Exception e)
            {
                s_logger.LogError(e.Message);
                return 1;
            }
        }

        private static async Task RunMultiplePhases(CryptoLiveParameters cryptoLiveParameters)
        {
            ICryptoBotPhasesFactory cryptoBotPhasesFactory = CreateCryptoBotPhasesGenerator(cryptoLiveParameters);
            Task[] tasks = new Task[cryptoLiveParameters.Currencies.Length];
            for (int i = 0; i < tasks.Length; i++)
            {
                CryptoBot cryptoBot = CreateCryptoBot(cryptoLiveParameters, cryptoBotPhasesFactory, cryptoLiveParameters.Currencies[i]);
                tasks[i] = RunMultiplePhasesPerCurrency(cryptoBot);
            }

            await Task.WhenAll(tasks);
        }

        private static CryptoBot CreateCryptoBot(CryptoLiveParameters cryptoLiveParameters, ICryptoBotPhasesFactory cryptoBotPhasesFactory, string currency) =>
            new CryptoBot(cryptoBotPhasesFactory, currency, 
                cryptoLiveParameters.MaxRsiToNotify,
                cryptoLiveParameters.CandleSize, 
                cryptoLiveParameters.CandleSize, 
                cryptoLiveParameters.CandleSize, 
                60, 
                cryptoLiveParameters.PriceChangeToNotify,
                1,
            cryptoLiveParameters.RsiCandlesAmount);

        private static ICryptoBotPhasesFactory CreateCryptoBotPhasesGenerator(CryptoLiveParameters cryptoLiveParameters)
        {
            ISystemClock systemClock = new SystemClock();
            INotificationService notificationService = NotificationServiceFactory.CreateNotificationService(
                cryptoLiveParameters.NotificationType, 
                cryptoLiveParameters.TwilioWhatsAppSender, 
                cryptoLiveParameters.WhatsAppRecipient,
                cryptoLiveParameters.TwilioSsid,
                cryptoLiveParameters.TwilioAuthToken);
            ICurrencyService currencyService = new BinanceCurrencyService(cryptoLiveParameters.BinanceApiKey, cryptoLiveParameters.BinanceApiSecretKey);
            return new CryptoBotPhasesFactory(notificationService, currencyService, systemClock);
        }

        private static async Task RunMultiplePhasesPerCurrency(CryptoBot cryptoBot)
        {
            int gainCounter = 0;
            int lossCounter = 0;
            int noChangeCounter = 0;
            while(gainCounter + lossCounter < 10)
            {
                (BotResult botResult, DateTime _) = await cryptoBot.StartAsync(DateTime.Now);
                switch (botResult)
                {
                    case BotResult.Gain:
                        gainCounter++;
                        break;
                    case BotResult.Even:
                        noChangeCounter++;
                        break;
                    case BotResult.Loss:
                        lossCounter++;
                        break;
                }
            }

            s_logger.LogInformation($"{cryptoBot.Currency}: Gain - {gainCounter}, Loss - {lossCounter}, NoChange: {noChangeCounter}");
        }
    }
}
