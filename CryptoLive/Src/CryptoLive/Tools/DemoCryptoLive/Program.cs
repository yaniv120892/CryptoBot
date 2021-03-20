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
        private static readonly DateTime s_botInitialTime = StorageWorkerInitialTimeProvider.DefaultStorageInitialTime.AddMinutes(120);
        private static readonly decimal s_initialQuoteOrderQuantity = 100;

        public static void Main()
        {
            var appParameters = AppParametersLoader<DemoCryptoParameters>.Load(s_configFile);
            s_logger.LogInformation(appParameters.ToString());
            RunMultiplePhases(appParameters).Wait();
        }

        private static async Task RunMultiplePhases(DemoCryptoParameters appParameters)
        {
            var systemClock = new DummySystemClock();
            var stopWatch = new DummyStopWatch();
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

            var demoCandleService = new DemoCandleService(appParameters.Currencies, 
                appParameters.CandlesDataFolder);
            var tradeService = new DemoTradeService(demoCandleService);


            await RunStorageWorkers(macdRepository, rsiRepository, candleRepository, systemClock, stopWatch, demoCandleService ,
                appParameters.Currencies , appParameters.CandleSize, appParameters.RsiSize, 
                appParameters.FastEmaSize, appParameters.SlowEmaSize, appParameters.SignalSize, appParameters.CalculatedDataFolder);
            ICurrencyBotFactory currencyBotFactory = CreateCurrencyBotFactory(appParameters, candleRepository, rsiRepository, macdRepository, systemClock, tradeService);

            var tasks = new Dictionary<string,Task<(int, int, string, decimal)>>();
            foreach (string currency in appParameters.Currencies)
            {
                tasks[currency] = RunMultiplePhasesPerCurrency(currencyBotFactory, currency, appParameters.RsiMemorySize);
            }

            await Task.WhenAll(tasks.Values);
            await PrintResults(appParameters, tasks);
        }

        private static ICurrencyBotFactory CreateCurrencyBotFactory(DemoCryptoParameters appParameters,
            RepositoryImpl<CandleStorageObject> candleRepository, RepositoryImpl<RsiStorageObject> rsiRepository,
            RepositoryImpl<MacdStorageObject> macdRepository,
            DummySystemClock systemClock, 
            ITradeService tradeService)
        {
            ICryptoBotPhasesFactory cryptoBotPhasesFactory = CreateCryptoPhasesFactory(candleRepository, rsiRepository,
                macdRepository, systemClock, tradeService);
            var currencyBotPhasesExecutorFactory = new CurrencyBotPhasesExecutorFactory();
            CurrencyBotPhasesExecutor currencyBotPhasesExecutor =
                currencyBotPhasesExecutorFactory.Create(cryptoBotPhasesFactory, appParameters);
            var accountQuoteProvider = new AccountQuoteProvider(new DemoAccountService());
            var currencyBotFactory = new CurrencyBotFactory(currencyBotPhasesExecutor, new EmptyNotificationService(), accountQuoteProvider);
            return currencyBotFactory;
        }

        private static async Task PrintResults(DemoCryptoParameters appParameters, Dictionary<string, Task<(int, int, string, decimal)>> tasks)
        {
            int totalWinCounter = 0;
            int totalLossCounter = 0;
            decimal totalEndQuoteOrderQuantity = 0;
            decimal totalInitialQuoteOrderQuantity = s_initialQuoteOrderQuantity * appParameters.Currencies.Length;
            int total;
            
            foreach (string currency in appParameters.Currencies)
            {
                (int winCounter, int lossCounter, string _, decimal quoteOrderQuantity) = await tasks[currency];
                total = winCounter + lossCounter;
                s_logger.LogInformation(
                    $"{currency} Summary: " +
                    $"Success {CalculateSuccess(winCounter, total)}%, " +
                    $"Return {CalculateReturn(quoteOrderQuantity, s_initialQuoteOrderQuantity)}%, " +
                    $"QuoteOrderQuantity: {quoteOrderQuantity}$, " +
                    $"Win - {winCounter}, " +
                    $"Loss {lossCounter}, " +
                    $"Total {total}");
                //s_logger.LogInformation(winAndLossDescriptions);
                totalWinCounter += winCounter;
                totalLossCounter += lossCounter;
                totalEndQuoteOrderQuantity += quoteOrderQuantity;
            }

            total = totalWinCounter + totalLossCounter;
            decimal totalSuccess = CalculateSuccess(totalWinCounter, total);
            decimal totalReturn = CalculateReturn(totalEndQuoteOrderQuantity, totalInitialQuoteOrderQuantity);
            s_logger.LogInformation(
                $"Final Summary: " +
                $"Success: {totalSuccess}%, " +
                $"Return: {totalReturn}%, " +
                $"QuoteOrderQuantity: {totalEndQuoteOrderQuantity}$, " +
                $"Win - {totalWinCounter}, " +
                $"Loss {totalLossCounter}, " +
                $"Total {total}");
        }

        private static decimal CalculateReturn(decimal endQuantity,
            decimal initialQuantity)
        {
            if (endQuantity == initialQuantity)
            {
                return 0;
            }
            if (endQuantity > initialQuantity)
            {
                return Math.Round(((endQuantity - initialQuantity) / initialQuantity) * 100, 3);
            }
            
            return Math.Round(((initialQuantity - endQuantity) / initialQuantity) * 100, 3)*(-1);
        }

        private static decimal CalculateSuccess(int winCounter, int winAndLossCounter) => 
            winAndLossCounter == 0 ? 0 : winCounter * 100 / winAndLossCounter;

        private static async Task RunStorageWorkers(IRepository<MacdStorageObject> macdRepository,
            IRepository<RsiStorageObject> rsiRepository,
            IRepository<CandleStorageObject> candleRepository,
            ISystemClock systemClock,
            IStopWatch stopWatch,
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
                    systemClock, stopWatch, CancellationToken.None, candleSize, calculatedDataFolder);
                DateTime storageInitialTime = StorageWorkerInitialTimeProvider.GetStorageInitialTime(currency, candleRepository);
                storageWorkersTasks[i] = storageWorker.StartAsync(storageInitialTime);
            }

            await Task.WhenAll(storageWorkersTasks);
        }

        private static StorageWorker CreateStorageWorker(IRepository<RsiStorageObject> rsiRepository,
            IRepository<WsmaStorageObject> wsmRepository,
            string currency,
            int rsiSize,
            IRepository<MacdStorageObject> macdRepository,
            IRepository<EmaAndSignalStorageObject> emaAndSignalStorageObject,
            int fastEmaSize,
            int slowEmaSize,
            int signalSize,
            IRepository<CandleStorageObject> candleRepository,
            ICandlesService candlesService,
            ISystemClock systemClock,
            IStopWatch stopWatch,
            CancellationToken cancellationToken,
            int candleSize, 
            string calculatedDataFolder)
        {
            INotificationService notificationService = new EmptyNotificationService();
            var rsiRepositoryUpdater = new RsiRepositoryUpdater(rsiRepository, wsmRepository, currency, rsiSize, calculatedDataFolder);
            var macdRepositoryUpdater = new MacdRepositoryUpdater(macdRepository, emaAndSignalStorageObject,
                currency,
                fastEmaSize, slowEmaSize, signalSize, calculatedDataFolder);
            var candleRepositoryUpdater =
                new CandleRepositoryUpdater(candleRepository, currency, candleSize, calculatedDataFolder);
            var storageWorker = new StorageWorker(notificationService, candlesService,
                systemClock,
                stopWatch,
                rsiRepositoryUpdater,
                candleRepositoryUpdater,
                macdRepositoryUpdater,
                cancellationToken,
                candleSize,
                currency,
                true,
                60);
            return storageWorker;        
        }

        private static ICryptoBotPhasesFactory CreateCryptoPhasesFactory(
            IRepository<CandleStorageObject> candleRepository,
            IRepository<RsiStorageObject> rsiRepository, IRepository<MacdStorageObject> macdRepository,
            ISystemClock systemClock,
            ITradeService tradeService)
        {
            var candlesProvider = new CandlesProvider(candleRepository);
            var rsiProvider = new RsiProvider(rsiRepository);
            var macdProvider = new MacdProvider(macdRepository);
            var currencyDataProvider =
                new CurrencyDataProvider(candlesProvider, rsiProvider, macdProvider);
            var cryptoBotPhasesFactory = new CryptoBotPhasesFactory(currencyDataProvider, systemClock, tradeService);
            return cryptoBotPhasesFactory;
        }

        private static async Task<(int winCounter, int lossCounter, string winAndLossDescriptions, decimal quoteOrderQuantity)>
            RunMultiplePhasesPerCurrency(ICurrencyBotFactory currencyBotFactory,
                string currency, 
                int appParametersRsiMemorySize)
        {
            int winCounter = 0;
            int lossCounter = 0;
            DateTime currentTime = s_botInitialTime;
            bool gotException = false;
            bool foundFaultedResult = false;
            var winPhaseDetails = new List<List<string>>();
            var lossesPhaseDetails = new List<List<string>>();
            decimal quoteOrderQuantity = s_initialQuoteOrderQuantity;
            while(!gotException && !foundFaultedResult)
            {
                try
                {
                    var queue = new CryptoFixedSizeQueueImpl<PriceAndRsi>(appParametersRsiMemorySize);
                    var cancellationTokenSource = new CancellationTokenSource();
                    ICurrencyBot currencyBot = currencyBotFactory.Create(queue, currency, cancellationTokenSource, currentTime);
                    BotResultDetails botResultDetails = await currencyBot.StartAsync();
                    currentTime = botResultDetails.EndTime;
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
            return (winCounter, lossCounter, winAndLossDescriptions, quoteOrderQuantity);
        }
    }
}
