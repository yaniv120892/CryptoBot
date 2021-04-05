using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.CryptoQueue;
using Common.DataStorageObjects;
using CryptoBot;
using CryptoBot.Abstractions;
using CryptoBot.Abstractions.Factories;
using CryptoBot.Factories;
using Infra;
using Infra.NotificationServices;
using Microsoft.Extensions.Logging;
using Services.Abstractions;
using Storage;
using Storage.Abstractions.Repository;
using Storage.Providers;
using Storage.Repository;
using Storage.Updaters;
using Storage.Workers;
using Utils.Abstractions;
using Utils.StopWatches;
using Utils.SystemClocks;

namespace DemoCryptoLive
{
    public class Program
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<Program>();
        private static readonly string s_configFile = "appsettings.json";
        private static readonly decimal s_initialQuoteOrderQuantity = 100;
        private static DemoCryptoParameters s_appParameters;

        public static void Main()
        {
            s_appParameters = AppParametersLoader<DemoCryptoParameters>.Load(s_configFile);
            RunMultiplePhases().Wait();
        }

        private static async Task RunMultiplePhases()
        {
            var systemClock = new DummySystemClock();
            var stopWatch = new DummyStopWatch();
            var candleRepository = new RepositoryImpl<CandleStorageObject>(s_appParameters.Currencies
                    .ToDictionary(currency=> currency, 
                        currency=> 
                            CalculatedFileProvider.GetCalculatedCandleFile(currency, s_appParameters.CalculatedDataFolder)), 
                deleteOldData: false);
            var rsiRepository = new RepositoryImpl<RsiStorageObject>(s_appParameters.Currencies
                    .ToDictionary(currency=> currency, 
                        currency=> 
                            CalculatedFileProvider.GetCalculatedRsiFile(currency, s_appParameters.RsiSize  ,s_appParameters.CalculatedDataFolder)), 
                deleteOldData: false);
            
            var meanAverageRepository = new RepositoryImpl<MeanAverageStorageObject>(s_appParameters.Currencies
                    .ToDictionary(currency=> currency, 
                        currency=> 
                            CalculatedFileProvider.GetCalculatedMeanAverageFile(currency, s_appParameters.MeanAverageSize  ,s_appParameters.CalculatedDataFolder)), 
                deleteOldData: false);

            var demoCandleService = new DemoCandleService(s_appParameters.Currencies, 
                s_appParameters.CandlesDataFolder, 
                s_appParameters.BotEndTime);
            var candlesProvider = new CandlesProvider(candleRepository);
            var tradeService = new DemoTradeService(candlesProvider);

            await RunStorageWorkers(rsiRepository, meanAverageRepository, candleRepository, systemClock, stopWatch, demoCandleService);
            ICurrencyBotFactory currencyBotFactory = CreateCurrencyBotFactory(candleRepository, rsiRepository, meanAverageRepository, systemClock, tradeService);

            var tasks = new Dictionary<string,Task<(int, int, int, string, decimal)>>();
            foreach (string currency in s_appParameters.Currencies)
            {
                tasks[currency] = RunMultiplePhasesPerCurrency(currencyBotFactory, currency);
            }

            await Task.WhenAll(tasks.Values);
            await ReportGenerator.GenerateReport(tasks, s_appParameters.Currencies, s_appParameters.PriceChangeToNotify);
        }

        private static ICurrencyBotFactory CreateCurrencyBotFactory(IRepository<CandleStorageObject> candleRepository,
            IRepository<RsiStorageObject> rsiRepository,
            IRepository<MeanAverageStorageObject> meanAverageRepository,
            DummySystemClock systemClock, 
            ITradeService tradeService)
        {
            ICryptoBotPhasesFactory cryptoBotPhasesFactory = CreateCryptoPhasesFactory(candleRepository, rsiRepository, 
                meanAverageRepository, systemClock, tradeService);
            var currencyBotPhasesExecutorFactory = new CurrencyBotPhasesExecutorFactory();
            CurrencyBotPhasesExecutor currencyBotPhasesExecutor =
                currencyBotPhasesExecutorFactory.Create(cryptoBotPhasesFactory, s_appParameters);
            var accountQuoteProvider = new AccountQuoteProvider(new DemoAccountService());
            var currencyBotFactory = new CurrencyBotFactory(currencyBotPhasesExecutor, new EmptyNotificationService(), accountQuoteProvider);
            return currencyBotFactory;
        }

        private static async Task RunStorageWorkers(IRepository<RsiStorageObject> rsiRepository,
            IRepository<MeanAverageStorageObject> meanAverageRepository,
            IRepository<CandleStorageObject> candleRepository,
            ISystemClock systemClock,
            IStopWatch stopWatch,
            ICandlesService candlesService)
        {
            var wsmRepository = new RepositoryImpl<WsmaStorageObject>(s_appParameters.Currencies.ToDictionary(currency=> currency, 
                    currency=> 
                        CalculatedFileProvider.GetCalculatedWsmaFile(currency, s_appParameters.RsiSize ,s_appParameters.CalculatedDataFolder)), 
                deleteOldData: false);

            var storageWorkersTasks = new Task[s_appParameters.Currencies.Length];
            for (int i = 0; i < storageWorkersTasks.Length; i++)
            {
                string currency = s_appParameters.Currencies[i];
                StorageWorker storageWorker = CreateStorageWorker(rsiRepository, wsmRepository, currency, meanAverageRepository, candleRepository, candlesService,
                    systemClock, stopWatch, CancellationToken.None);
                DateTime storageInitialTime = StorageWorkerInitialTimeProvider.GetStorageInitialTime(currency, candleRepository,
                    s_appParameters.BotStartTime, s_appParameters.BotEndTime);
                storageWorkersTasks[i] = storageWorker.StartAsync(storageInitialTime);
            }

            await Task.WhenAll(storageWorkersTasks);
        }

        private static StorageWorker CreateStorageWorker(IRepository<RsiStorageObject> rsiRepository,
            IRepository<WsmaStorageObject> wsmRepository,
            string currency,
            IRepository<MeanAverageStorageObject> meanAverageRepository,
            IRepository<CandleStorageObject> candleRepository,
            ICandlesService candlesService,
            ISystemClock systemClock,
            IStopWatch stopWatch,
            CancellationToken cancellationToken)
        {
            INotificationService notificationService = new EmptyNotificationService();
            var rsiRepositoryUpdater = new RsiRepositoryUpdater(rsiRepository, wsmRepository, currency, s_appParameters.RsiSize, s_appParameters.CalculatedDataFolder);
            var candleRepositoryUpdater =
                new CandleRepositoryUpdater(candleRepository, currency, s_appParameters.CalculatedDataFolder);
            var meanAverageRepositoryUpdater = new MeanAverageRepositoryUpdater(meanAverageRepository, candleRepository, currency, s_appParameters.MeanAverageSize, s_appParameters.CalculatedDataFolder);
            var storageWorker = new StorageWorker(notificationService, candlesService,
                systemClock,
                stopWatch,
                rsiRepositoryUpdater,
                candleRepositoryUpdater,
                meanAverageRepositoryUpdater,
                cancellationToken,
                s_appParameters.CandleSize,
                currency,
                true,
                60);
            return storageWorker;        
        }

        private static ICryptoBotPhasesFactory CreateCryptoPhasesFactory(
            IRepository<CandleStorageObject> candleRepository,
            IRepository<RsiStorageObject> rsiRepository, 
            IRepository<MeanAverageStorageObject> meanAverageRepository,
            ISystemClock systemClock,
            ITradeService tradeService)
        {
            var candlesProvider = new CandlesProvider(candleRepository);
            var rsiProvider = new RsiProvider(rsiRepository);
            var meanAverageProvider = new MeanAverageProvider(meanAverageRepository);
            var currencyDataProvider =
                new CurrencyDataProvider(candlesProvider, rsiProvider, meanAverageProvider);
            var cryptoBotPhasesFactory = new CryptoBotPhasesFactory(currencyDataProvider, systemClock, tradeService);
            return cryptoBotPhasesFactory;
        }

        private static async Task<(int winCounter, int lossCounter, int evenCounter, string winAndLossDescriptions, decimal quoteOrderQuantity)>
            RunMultiplePhasesPerCurrency(ICurrencyBotFactory currencyBotFactory,
                string currency)
        {
            int winCounter = 0;
            int lossCounter = 0;
            int evenCounter = 0;
            DateTime currentTime = s_appParameters.BotStartTime.AddMinutes(120);
            bool gotException = false;
            bool foundFaultedResult = false;
            var winPhaseDetails = new List<List<string>>();
            var lossesPhaseDetails = new List<List<string>>();
            var evensPhaseDetails = new List<List<string>>();
            decimal quoteOrderQuantity = s_initialQuoteOrderQuantity;
            while(!gotException && !foundFaultedResult)
            {
                try
                {
                    var queue = new CryptoFixedSizeQueueImpl<PriceAndRsi>(s_appParameters.RsiMemorySize);
                    var cancellationTokenSource = new CancellationTokenSource();
                    var parentCancellationToken = new Queue<CancellationToken>();
                    ICurrencyBot currencyBot = currencyBotFactory.Create(queue, parentCancellationToken,
                        currency, cancellationTokenSource, currentTime);
                    BotResultDetails botResultDetails = await currencyBot.StartAsync();
                    currentTime = botResultDetails.EndTime;
                    switch (botResultDetails.BotResult)
                    {
                        case BotResult.Win:
                            winCounter++;
                            winPhaseDetails.Add(botResultDetails.PhasesDescription);
                            break;
                        case BotResult.Loss:
                            lossCounter++;
                            lossesPhaseDetails.Add(botResultDetails.PhasesDescription);
                            break;
                        case BotResult.Even:
                            evenCounter++;
                            evensPhaseDetails.Add(botResultDetails.PhasesDescription);
                            break;
                        case BotResult.Faulted:
                            foundFaultedResult = true;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                catch (Exception e)
                {
                    s_logger.LogError(e, "Bot Failure");
                    gotException = true;
                }
            }

            string winAndLossDescriptions = TestResultsSummary.BuildWinAndLossDescriptions(lossesPhaseDetails, winPhaseDetails);
            return (winCounter, lossCounter, evenCounter, winAndLossDescriptions, quoteOrderQuantity);
        }
    }
}
