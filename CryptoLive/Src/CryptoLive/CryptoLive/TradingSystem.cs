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
using CryptoLive.Abstractions;
using Infra;
using Microsoft.Extensions.Logging;
using Services;
using Services.Abstractions;
using Storage.Abstractions.Repository;
using Storage.Providers;
using Storage.Repository;
using Storage.Updaters;
using Storage.Workers;
using Utils.Abstractions;
using Utils.StopWatches;
using Utils.SystemClocks;

namespace CryptoLive
{
    public class TradingSystem : ITradingSystem
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<TradingSystem>();

        private readonly INotificationService m_notificationService;
        private readonly CryptoLiveParameters m_cryptoLiveParameters;
        private readonly CancellationTokenSource m_systemCancellationTokenSource;
        private IBotResultDetailsRepository m_botResultsRepository;

        public TradingSystem(INotificationService notificationService,
            CryptoLiveParameters cryptoLiveParameters, 
            CancellationTokenSource systemCancellationTokenSource)
        {
            m_notificationService = notificationService;
            m_systemCancellationTokenSource = systemCancellationTokenSource;
            m_cryptoLiveParameters = cryptoLiveParameters;
        }

        public async Task Run()
        { 
            var systemClock = new SystemClock();
            var stopWatchWrapper = new StopWatchWrapper();
            var systemClockWithDelay = new SystemClockWithDelay(systemClock ,m_cryptoLiveParameters.BotDelayTimeInMinutes);

            var currencyClientFactory = new CurrencyClientFactory(m_cryptoLiveParameters.BinanceApiKey, 
                m_cryptoLiveParameters.BinanceApiSecretKey);
            var candlesService = new BinanceCandleService(currencyClientFactory);
            var tradeService = new BinanceTradeService(currencyClientFactory);

            var candleRepository = new RepositoryImpl<CandleStorageObject>(m_cryptoLiveParameters.Currencies.ToDictionary(currency=> currency));
            var rsiRepository = new RepositoryImpl<RsiStorageObject>(m_cryptoLiveParameters.Currencies.ToDictionary(currency=> currency));
            var wsmRepository = new RepositoryImpl<WsmaStorageObject>(m_cryptoLiveParameters.Currencies.ToDictionary(currency=> currency));
            
            var meanAverageRepository = new RepositoryImpl<MeanAverageStorageObject>(m_cryptoLiveParameters.Currencies.ToDictionary(currency=> currency));

            m_botResultsRepository = new MongoBotResultDetailsRepository(m_cryptoLiveParameters.CryptoBotName,
                m_cryptoLiveParameters.MongoDbHost, m_cryptoLiveParameters.MongoDbDataBase);
            int rsiSize = m_cryptoLiveParameters.RsiSize;
            int candleSize = m_cryptoLiveParameters.CandleSize;
            int meanAverageSize = m_cryptoLiveParameters.MeanAverageSize;
            
            ICryptoBotPhasesFactory cryptoBotPhasesFactory = CreateCryptoPhasesFactory(candleRepository, rsiRepository, meanAverageRepository,
                systemClockWithDelay, tradeService);
            var currencyBotPhasesExecutorFactory = new CurrencyBotPhasesExecutorFactory();
            ICurrencyBotPhasesExecutor currencyBotPhasesExecutor =  
                currencyBotPhasesExecutorFactory.Create(cryptoBotPhasesFactory, m_cryptoLiveParameters);
            var accountQuoteProvider = new AccountQuoteProvider(new BinanceAccountService(currencyClientFactory));
            var currencyBotFactory = new CurrencyBotFactory(currencyBotPhasesExecutor, m_notificationService, accountQuoteProvider);
            
            var currencyBotTasks = new Task[m_cryptoLiveParameters.Currencies.Length];
            var storageWorkersTasks = new Task[m_cryptoLiveParameters.Currencies.Length];
            
            for (int i = 0; i < currencyBotTasks.Length; i++)
            {
                string currency = m_cryptoLiveParameters.Currencies[i];
                StorageWorker storageWorker = CreateStorageWorker(rsiRepository, wsmRepository, currency, rsiSize, candleRepository, meanAverageRepository, meanAverageSize, candlesService,
                    systemClock, stopWatchWrapper, candleSize);
                DateTime storageStartTime = await systemClock.Wait(CancellationToken.None, currency, 0, "Init",DateTime.UtcNow);
                storageWorkersTasks[i] = storageWorker.StartAsync(storageStartTime);
                await systemClock.Wait(CancellationToken.None, currency, m_cryptoLiveParameters.BotDelayTimeInMinutes*60, "Init2",storageStartTime);
                currencyBotTasks[i] = RunMultiplePhasesPerCurrency(currencyBotFactory, 
                    currency, 
                    storageStartTime, 
                    m_cryptoLiveParameters.RsiMemorySize);
            }

            await Task.WhenAll(currencyBotTasks);        
        }
        
        public void Stop()
        {
            m_systemCancellationTokenSource.Cancel();
        }

        private StorageWorker CreateStorageWorker(IRepository<RsiStorageObject> rsiRepository, IRepository<WsmaStorageObject> wsmRepository,
            string currency, int rsiSize, IRepository<CandleStorageObject> candleRepository,
            IRepository<MeanAverageStorageObject> meanAverageRepository, int meanAverageSize,
            ICandlesService candlesService, ISystemClock systemClock, IStopWatch stopWatchWrapper,
            int candleSize)
        {
            var rsiRepositoryUpdater = new RsiRepositoryUpdater(rsiRepository, wsmRepository, currency, rsiSize, string.Empty);
            var candleRepositoryUpdater = new CandleRepositoryUpdater(candleRepository, currency,string.Empty);
            var meanAverageRepositoryUpdater = new MeanAverageRepositoryUpdater(meanAverageRepository, candleRepository, currency, meanAverageSize, string.Empty);
            var storageWorker = new StorageWorker(m_notificationService,
                candlesService,
                systemClock,
                stopWatchWrapper,
                rsiRepositoryUpdater,
                candleRepositoryUpdater,
                meanAverageRepositoryUpdater,
                m_systemCancellationTokenSource.Token,
                candleSize,
                currency,
                false,
                30);
            return storageWorker;
        }

        private async Task RunMultiplePhasesPerCurrency(ICurrencyBotFactory currencyBotFactory,
            string currency,
            DateTime storageStartTime,
            int rsiMemorySize)
        {
            DateTime botStartTime = storageStartTime;
            while(!m_systemCancellationTokenSource.IsCancellationRequested)
            {
                var botCancellationTokenSource = new CancellationTokenSource();
                var queue = new CryptoFixedSizeQueueImpl<PriceAndRsi>(rsiMemorySize);
                var isParentsRunningCancellationToken = new Queue<CancellationToken>();
                ICurrencyBot currencyBot = currencyBotFactory.Create(queue, isParentsRunningCancellationToken, 
                    currency, botCancellationTokenSource, botStartTime);
                BotResultDetails botResultDetails = await currencyBot.StartAsync();
                await m_botResultsRepository.AddAsync(botResultDetails);
                botStartTime = botResultDetails.EndTime;
                if (botResultDetails.BotResult == BotResult.Faulted)
                {
                    s_logger.LogWarning($"Bot execution was faulted, Error:{botResultDetails.Exception.Message}");
                    m_notificationService.Notify($"CurrencyBot {currency} stopped");
                    break;
                }

                s_logger.LogInformation(botResultDetails.ToString());
            }
            s_logger.LogInformation($"{currency} Storage worker got cancellation request");
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
            var cryptoBotPhasesFactoryCreator = new CryptoBotPhasesFactoryCreator(
                systemClock, 
                rsiProvider, 
                candlesProvider, 
                meanAverageProvider,
                tradeService);
            return cryptoBotPhasesFactoryCreator.Create();
        }
    }
}