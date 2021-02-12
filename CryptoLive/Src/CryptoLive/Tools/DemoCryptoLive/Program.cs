using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.DataStorageObjects;
using CryptoBot;
using CryptoBot.Abstractions;
using CryptoBot.Factories;
using Infra;
using Infra.NotificationServices;
using Microsoft.Extensions.Logging;
using Services.Abstractions;
using Storage;
using Storage.Abstractions;
using Storage.Abstractions.Providers;
using Storage.Abstractions.Repository;
using Storage.Providers;
using Storage.Repository;
using Storage.Updaters;
using Storage.Workers;
using Utils.Abstractions;
using Utils.SystemClocks;

namespace DemoCryptoLive
{
    public class Program
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<Program>();
        private static readonly string s_configFile = "appsettings.json";
        private static readonly DateTime s_defaultStorageInitialTime = DateTime.Parse("2/2/2021  12:59:00 PM");
        private static DateTime s_botInitialTime;

        public static void Main()
        {
            DemoCryptoParameters appParameters = AppParametersLoader<DemoCryptoParameters>.Load(s_configFile);
            s_logger.LogInformation(appParameters.ToString());
            RunMultiplePhases(appParameters).Wait();
        }

        private static async Task RunMultiplePhases(DemoCryptoParameters appParameters)
        {
            var systemClock = new DummySystemClock();
            var candleRepository = new RepositoryImpl<CandleStorageObject>(appParameters.Currencies
                    .ToDictionary(currency=> currency, 
                        currency=> 
                            CalculatedFileProvider.GetCalculatedCandleFile(currency, appParameters.CandleSize  ,appParameters.CalculatedDataFolder)), 
                deleteOldData: false);
            var rsiRepository = new RepositoryImpl<RsiStorageObject>(appParameters.Currencies
                    .ToDictionary(currency=> currency, 
                        currency=> 
                            CalculatedFileProvider.GetCalculatedRsiFile(currency, appParameters.RsiSize  ,appParameters.CalculatedDataFolder)), 
                deleteOldData: false);
            var macdRepository = new RepositoryImpl<MacdStorageObject>(appParameters.Currencies
                    .ToDictionary(currency=> currency, 
                        currency=> 
                            CalculatedFileProvider.GetCalculatedMacdFile(currency, appParameters.SlowEmaSize, 
                                appParameters.FastEmaSize, appParameters.SignalSize ,appParameters.CalculatedDataFolder)), 
                deleteOldData: false);

            DemoCandleService demoCandleService = new DemoCandleService(appParameters.Currencies, 
                appParameters.CandlesDataFolder);

            await RunStorageWorkers(macdRepository, rsiRepository, candleRepository, systemClock, demoCandleService ,
                appParameters.Currencies , appParameters.CandleSize, appParameters.RsiSize, 
                appParameters.FastEmaSize, appParameters.SlowEmaSize, appParameters.SignalSize, appParameters.CalculatedDataFolder);
            ICryptoBotPhasesFactory cryptoBotPhasesFactory = CreateCryptoPhasesFactory(candleRepository, rsiRepository, 
                macdRepository, systemClock, demoCandleService);
            
            var tasks = new Dictionary<string,Task<(int, int, string)>>();
            foreach (string currency in appParameters.Currencies)
            {
                CurrencyBot currencyBot = DemoCurrencyBotFactory.Create(appParameters, cryptoBotPhasesFactory, currency);
                tasks[currency] = RunMultiplePhasesPerCurrency(currencyBot);
            }

            await Task.WhenAll(tasks.Values);
            await PrintResults(appParameters, tasks);
        }

        private static async Task PrintResults(DemoCryptoParameters appParameters, Dictionary<string, Task<(int, int, string)>> tasks)
        {
            int totalWinCounter = 0;
            int totalLossCounter = 0;
            int total;
            
            foreach (string currency in appParameters.Currencies)
            {
                (int winCounter, int lossCounter, string winAndLossDescriptions) = await tasks[currency];
                total = winCounter + lossCounter;
                s_logger.LogInformation(
                    $"{currency} Summary: {(total == 0 ? 0 : winCounter * 100 / total)}%, Win - {winCounter}, Loss {lossCounter}, Total {total}");
                s_logger.LogInformation(winAndLossDescriptions);
                totalWinCounter += winCounter;
                totalLossCounter += lossCounter;
            }

            total = totalWinCounter + totalLossCounter;
            s_logger.LogInformation(
                $"Final Summary: {(total == 0 ? 0 : totalWinCounter * 100 / total)}%, Win - {totalWinCounter}, Loss {totalLossCounter}, Total {total}");
        }

        private static async Task RunStorageWorkers(RepositoryImpl<MacdStorageObject> macdRepository,
            RepositoryImpl<RsiStorageObject> rsiRepository,
            RepositoryImpl<CandleStorageObject> candleRepository,
            ISystemClock systemClock,
            ICandlesService candlesService,
            IReadOnlyList<string> currencies,
            int candleSize,
            int rsiSize,
            int fastEmaSize,
            int slowEmaSize,
            int signalSize,
            string calculatedDataFolder)
        {
            var wsmRepository = new RepositoryImpl<WsmaStorageObject>(currencies.ToDictionary(currency=> currency, 
                    currency=> 
                        CalculatedFileProvider.GetCalculatedWsmaFile(currency, rsiSize ,calculatedDataFolder)), 
                deleteOldData: false);
            var emaAndSignalStorageObject = new RepositoryImpl<EmaAndSignalStorageObject>(currencies.ToDictionary(currency=> currency, 
                    currency=> 
                        CalculatedFileProvider.GetCalculatedEmaAndSignalFile(currency, slowEmaSize, 
                            fastEmaSize, signalSize ,calculatedDataFolder)),
                deleteOldData: false);

            var storageWorkersTasks = new Task[currencies.Count];
            for (int i = 0; i < storageWorkersTasks.Length; i++)
            {
                string currency = currencies[i];
                StorageWorker storageWorker = CreateStorageWorker(rsiRepository, wsmRepository, currency, rsiSize, macdRepository,
                    emaAndSignalStorageObject, fastEmaSize, slowEmaSize, signalSize, candleRepository, candlesService,
                    systemClock, CancellationToken.None, candleSize, calculatedDataFolder);
                DateTime storageInitialTime = GetStorageInitialTime(currency, candleRepository);
                s_botInitialTime = s_defaultStorageInitialTime.AddMinutes(120);
                storageWorkersTasks[i] = storageWorker.StartAsync(storageInitialTime);
            }

            await Task.WhenAll(storageWorkersTasks);
        }

        private static DateTime GetStorageInitialTime(string currency, RepositoryImpl<CandleStorageObject> candleRepository)
        {
            DateTime lastSavedCandleTime = candleRepository.GetLastByTime(currency);
            if (default == lastSavedCandleTime)
            {
                return s_defaultStorageInitialTime;
            }

            return lastSavedCandleTime;
        }

        private static StorageWorker CreateStorageWorker(RepositoryImpl<RsiStorageObject> rsiRepository,
            RepositoryImpl<WsmaStorageObject> wsmRepository,
            string currency,
            int rsiSize,
            RepositoryImpl<MacdStorageObject> macdRepository,
            RepositoryImpl<EmaAndSignalStorageObject> emaAndSignalStorageObject,
            int fastEmaSize,
            int slowEmaSize,
            int signalSize,
            RepositoryImpl<CandleStorageObject> candleRepository,
            ICandlesService candlesService,
            ISystemClock systemClock,
            CancellationToken cancellationToken,
            int candleSize, 
            string calculatedDataFolder)
        {
            IRepositoryUpdater rsiRepositoryUpdater = new RsiRepositoryUpdater(rsiRepository, wsmRepository, currency, rsiSize, calculatedDataFolder);
            IRepositoryUpdater macdRepositoryUpdater = new MacdRepositoryUpdater(macdRepository, emaAndSignalStorageObject,
                currency,
                fastEmaSize, slowEmaSize, signalSize, calculatedDataFolder);
            IRepositoryUpdater candleRepositoryUpdater =
                new CandleRepositoryUpdater(candleRepository, currency, candleSize, calculatedDataFolder);
            var storageWorker = new StorageWorker(candlesService,
                systemClock,
                rsiRepositoryUpdater,
                candleRepositoryUpdater,
                macdRepositoryUpdater,
                cancellationToken,
                candleSize,
                currency,
                StorageWorkerMode.Demo);
            return storageWorker;        
        }

        private static ICryptoBotPhasesFactory CreateCryptoPhasesFactory(RepositoryImpl<CandleStorageObject> candleRepository,
            RepositoryImpl<RsiStorageObject> rsiRepository, RepositoryImpl<MacdStorageObject> macdRepository, ISystemClock systemClock,
            IPriceService priceService)
        {
            IPriceProvider priceProvider = new PriceProvider(priceService);
            ICandlesProvider candlesProvider = new CandlesProvider(candleRepository);
            IRsiProvider rsiProvider = new RsiProvider(rsiRepository);
            IMacdProvider macdProvider = new MacdProvider(macdRepository);
            ICurrencyDataProvider currencyDataProvider =
                new CurrencyDataProvider(priceProvider, candlesProvider, rsiProvider, macdProvider);
            INotificationService notificationService = new EmptyNotificationService();
            ICryptoBotPhasesFactory cryptoBotPhasesFactory = new CryptoBotPhasesFactory(currencyDataProvider, systemClock, notificationService);
            return cryptoBotPhasesFactory;
        }

        private static async Task<(int winCounter, int lossCounter, string winAndLossDescriptions)> RunMultiplePhasesPerCurrency(CurrencyBot currencyBot)
        {
            int winCounter = 0;
            int lossCounter = 0;
            DateTime currentTime = s_botInitialTime;
            bool gotException = false;
            var winPhaseDetails = new List<List<string>>();
            var lossesPhaseDetails = new List<List<string>>();
            while(!gotException)
            {
                try
                {
                    BotResultDetails botResultDetails;
                    (botResultDetails, currentTime) = await currencyBot.StartAsync(currentTime);
                    switch (botResultDetails.BotResult)
                    {
                        case BotResult.Win:
                            winCounter++;
                            winPhaseDetails.Add(botResultDetails.PhasesDescription);
                            break;
                        case BotResult.Even:
                            break;
                        case BotResult.Loss:
                            lossCounter++;
                            lossesPhaseDetails.Add(botResultDetails.PhasesDescription);
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

            string winAndLossDescriptions = BuildWinAndLossDescriptions(lossesPhaseDetails, winPhaseDetails);
            return (winCounter, lossCounter, winAndLossDescriptions);
        }

        private static string BuildWinAndLossDescriptions(List<List<string>> lossesPhaseDetails, List<List<string>> winPhaseDetails)
        {
            StringBuilder lossesDescription = new StringBuilder();
            lossesDescription.AppendLine("Losses phases details:");
            for (int i = 0; i < lossesPhaseDetails.Count; i++)
            {
                lossesDescription.AppendLine($"Loss {i}:");
                foreach (string phase in lossesPhaseDetails[i])
                {
                    lossesDescription.AppendLine(phase);
                }

                lossesDescription.AppendLine();
            }

            StringBuilder winsDescription = new StringBuilder();
            winsDescription.AppendLine("Wins phases details:");
            for (int i = 0; i < winPhaseDetails.Count; i++)
            {
                winsDescription.AppendLine($"Win {i}:");
                foreach (string phase in winPhaseDetails[i])
                {
                    winsDescription.AppendLine(phase);
                }

                winsDescription.AppendLine();
            }

            string winAndLossDescriptions = $"{winsDescription}\n{lossesDescription}";
            return winAndLossDescriptions;
        }
    }
}
