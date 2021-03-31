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
            var systemClockWithDelay = new SystemClockWithDelay(systemClock ,m_cryptoLiveParameters.BotDelayTime);

            var currencyClientFactory = new CurrencyClientFactory(m_cryptoLiveParameters.BinanceApiKey, 
                m_cryptoLiveParameters.BinanceApiSecretKey);
            var candlesService = new BinanceCandleService(currencyClientFactory);
            var tradeService = new BinanceTradeService(currencyClientFactory);

            var candleRepository = new RepositoryImpl<CandleStorageObject>(m_cryptoLiveParameters.Currencies.ToDictionary(currency=> currency));
            var rsiRepository = new RepositoryImpl<RsiStorageObject>(m_cryptoLiveParameters.Currencies.ToDictionary(currency=> currency));
            var wsmRepository = new RepositoryImpl<WsmaStorageObject>(m_cryptoLiveParameters.Currencies.ToDictionary(currency=> currency));
            var macdRepository = new RepositoryImpl<MacdStorageObject>(m_cryptoLiveParameters.Currencies.ToDictionary(currency=> currency));
            var emaAndSignalStorageObject = new RepositoryImpl<EmaAndSignalStorageObject>(m_cryptoLiveParameters.Currencies.ToDictionary(currency=> currency));
            m_botResultsRepository = new MongoBotResultDetailsRepository(m_cryptoLiveParameters.CryptoBotName,
                m_cryptoLiveParameters.MongoDbHost, m_cryptoLiveParameters.MongoDbDataBase);
            int rsiSize = m_cryptoLiveParameters.RsiSize;
            int fastEmaSize = m_cryptoLiveParameters.FastEmaSize;
            int slowEmaSize = m_cryptoLiveParameters.SlowEmaSize;
            int signalSize = m_cryptoLiveParameters.SignalSize;
            int candleSize = m_cryptoLiveParameters.CandleSize;
            
            ICryptoBotPhasesFactory cryptoBotPhasesFactory = CreateCryptoPhasesFactory(candleRepository, rsiRepository, 
                macdRepository, systemClockWithDelay, tradeService);
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
                StorageWorker storageWorker = CreateStorageWorker(rsiRepository, wsmRepository, currency, rsiSize, macdRepository,
                    emaAndSignalStorageObject, fastEmaSize, slowEmaSize, signalSize, candleRepository, candlesService,
                    systemClock, stopWatchWrapper, candleSize);
                DateTime storageStartTime = await systemClock.Wait(CancellationToken.None, currency, 0, "Init",DateTime.UtcNow);
                storageWorkersTasks[i] = storageWorker.StartAsync(storageStartTime);
                await systemClock.Wait(CancellationToken.None, currency, m_cryptoLiveParameters.BotDelayTime*60, "Init2",storageStartTime);
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
            string currency, int rsiSize, IRepository<MacdStorageObject> macdRepository, IRepository<EmaAndSignalStorageObject> emaAndSignalStorageObject,
            int fastEmaSize, int slowEmaSize, int signalSize, IRepository<CandleStorageObject> candleRepository,
            ICandlesService candlesService, ISystemClock systemClock, IStopWatch stopWatchWrapper,
            int candleSize)
        {
            var rsiRepositoryUpdater = new RsiRepositoryUpdater(rsiRepository, wsmRepository, currency, rsiSize, string.Empty);
            var macdRepositoryUpdater = new MacdRepositoryUpdater(macdRepository, emaAndSignalStorageObject,
                currency, fastEmaSize, slowEmaSize, signalSize, string.Empty);
            var candleRepositoryUpdater = new CandleRepositoryUpdater(candleRepository, currency, candleSize,string.Empty);
            
            var storageWorker = new StorageWorker(m_notificationService,
                candlesService,
                systemClock,
                stopWatchWrapper,
                rsiRepositoryUpdater,
                candleRepositoryUpdater,
                macdRepositoryUpdater,
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
            IRepository<MacdStorageObject> macdRepository,
            ISystemClock systemClock, 
            ITradeService tradeService)
        {
            var candlesProvider = new CandlesProvider(candleRepository);
            var rsiProvider = new RsiProvider(rsiRepository);
            var macdProvider = new MacdProvider(macdRepository);
            var cryptoBotPhasesFactoryCreator = new CryptoBotPhasesFactoryCreator(
                systemClock, 
                rsiProvider, 
                candlesProvider, 
                macdProvider,
                tradeService);
            return cryptoBotPhasesFactoryCreator.Create();
        }
    }
}