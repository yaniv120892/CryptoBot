using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.DataStorageObjects;
using CryptoBot.Abstractions;
using CryptoBot.Abstractions.Factories;
using CryptoBot.Factories;
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
using Utils.Notifications;
using Utils.StopWatches;
using Utils.SystemClocks;

namespace CryptoLive
{
    public class Program
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<Program>();
        private static readonly string s_configFile = "appsettings.json";

        public static void Main()
        {
            CryptoLiveParameters appParameters = AppParametersLoader<CryptoLiveParameters>.Load(s_configFile);
            s_logger.LogInformation(appParameters.ToString());
            RunMultiplePhases(appParameters).Wait();
        }

        private static async Task RunMultiplePhases(CryptoLiveParameters cryptoLiveParameters)
        {
            var systemClock = new SystemClock();
            var stopWatchWrapper = new StopWatchWrapper();
            var systemClockWithDelay = new SystemClockWithDelay(systemClock ,cryptoLiveParameters.BotDelayTime);
            var notificationServiceFactory =
                new NotificationServiceFactory(cryptoLiveParameters.TwilioWhatsAppSender,
                    cryptoLiveParameters.WhatsAppRecipient, cryptoLiveParameters.TwilioSsid,
                    cryptoLiveParameters.TwilioAuthToken, cryptoLiveParameters.TelegramChatId, cryptoLiveParameters.TelegramAuthToken);
            INotificationService notificationService = notificationServiceFactory.Create(cryptoLiveParameters.NotificationType);

            CurrencyClientFactory currencyClientFactory = new CurrencyClientFactory(cryptoLiveParameters.BinanceApiKey, 
                cryptoLiveParameters.BinanceApiSecretKey);
            var candlesService = new BinanceCandleService(currencyClientFactory);
            var storageCancellationTokenSource = new CancellationTokenSource();

            var candleRepository = new RepositoryImpl<CandleStorageObject>(cryptoLiveParameters.Currencies.ToDictionary(currency=> currency));
            var rsiRepository = new RepositoryImpl<RsiStorageObject>(cryptoLiveParameters.Currencies.ToDictionary(currency=> currency));
            var wsmRepository = new RepositoryImpl<WsmaStorageObject>(cryptoLiveParameters.Currencies.ToDictionary(currency=> currency));
            var macdRepository = new RepositoryImpl<MacdStorageObject>(cryptoLiveParameters.Currencies.ToDictionary(currency=> currency));
            var emaAndSignalStorageObject = new RepositoryImpl<EmaAndSignalStorageObject>(cryptoLiveParameters.Currencies.ToDictionary(currency=> currency));
            
            int rsiSize = cryptoLiveParameters.RsiSize;
            int fastEmaSize = cryptoLiveParameters.FastEmaSize;
            int slowEmaSize = cryptoLiveParameters.SlowEmaSize;
            int signalSize = cryptoLiveParameters.SignalSize;
            int candleSize = cryptoLiveParameters.CandleSize;
            
            ICryptoBotPhasesFactory cryptoBotPhasesFactory = CreateCryptoPhasesFactory(candleRepository, rsiRepository, 
                macdRepository, systemClockWithDelay, currencyClientFactory);
            var currencyBotPhasesExecutorFactory = new CurrencyBotPhasesExecutorFactory();
            ICurrencyBotPhasesExecutor currencyBotPhasesExecutor =  currencyBotPhasesExecutorFactory.Create(cryptoBotPhasesFactory, cryptoLiveParameters);
            var currencyBotFactory = new CurrencyBotFactory(currencyBotPhasesExecutor, notificationService);
            
            var currencyBotTasks = new Task[cryptoLiveParameters.Currencies.Length];
            var storageWorkersTasks = new Task[cryptoLiveParameters.Currencies.Length];
            
            notificationService.Notify($"Start CryptoLive {Environment.MachineName} for currencies {string.Join(", ",cryptoLiveParameters.Currencies)}");
            for (int i = 0; i < currencyBotTasks.Length; i++)
            {
                string currency = cryptoLiveParameters.Currencies[i];
                StorageWorker storageWorker = CreateStorageWorker(rsiRepository, wsmRepository, currency, rsiSize, macdRepository,
                    emaAndSignalStorageObject, fastEmaSize, slowEmaSize, signalSize, candleRepository, candlesService,
                    systemClock, stopWatchWrapper, notificationService, storageCancellationTokenSource, candleSize);
                DateTime storageStartTime = await systemClock.Wait(CancellationToken.None, currency, 0, "Init",DateTime.UtcNow);
                storageWorkersTasks[i] = storageWorker.StartAsync(storageStartTime);
                await systemClock.Wait(CancellationToken.None, currency, cryptoLiveParameters.BotDelayTime*60, "Init2",storageStartTime);
                currencyBotTasks[i] = RunMultiplePhasesPerCurrency(currencyBotFactory, currency, storageStartTime, storageCancellationTokenSource);
            }

            await Task.WhenAll(currencyBotTasks);
            notificationService.Notify($"Done CryptoLive {Environment.MachineName} for currencies {string.Join(", ",cryptoLiveParameters.Currencies)}");
        }

        private static StorageWorker CreateStorageWorker(IRepository<RsiStorageObject> rsiRepository, IRepository<WsmaStorageObject> wsmRepository,
            string currency, int rsiSize, IRepository<MacdStorageObject> macdRepository, IRepository<EmaAndSignalStorageObject> emaAndSignalStorageObject,
            int fastEmaSize, int slowEmaSize, int signalSize, IRepository<CandleStorageObject> candleRepository,
            ICandlesService candlesService, ISystemClock systemClock, IStopWatch stopWatchWrapper, INotificationService notificationService, CancellationTokenSource cancellationTokenSource,
            int candleSize)
        {
            var rsiRepositoryUpdater = new RsiRepositoryUpdater(rsiRepository, wsmRepository, currency, rsiSize, string.Empty);
            var macdRepositoryUpdater = new MacdRepositoryUpdater(macdRepository, emaAndSignalStorageObject,
                currency, fastEmaSize, slowEmaSize, signalSize, string.Empty);
            var candleRepositoryUpdater = new CandleRepositoryUpdater(candleRepository, currency, candleSize,string.Empty);
            
            var storageWorker = new StorageWorker(notificationService,
                candlesService,
                systemClock,
                stopWatchWrapper,
                rsiRepositoryUpdater,
                candleRepositoryUpdater,
                macdRepositoryUpdater,
                cancellationTokenSource.Token,
                candleSize,
                currency,
                false,
                30);
            return storageWorker;
        }

        private static async Task RunMultiplePhasesPerCurrency(ICurrencyBotFactory currencyBotFactory, 
            string currency,
            DateTime storageStartTime, 
            CancellationTokenSource storageCancellationTokenSource)
        {
            CancellationTokenSource botCancellationTokenSource = new CancellationTokenSource();
            ICurrencyBot currencyBot = currencyBotFactory.Create(currency, botCancellationTokenSource, storageStartTime);
            while(!storageCancellationTokenSource.IsCancellationRequested)
            {
                (BotResultDetails botResultDetails, DateTime _) = await currencyBot.StartAsync();
                s_logger.LogInformation(botResultDetails.ToString());
            }
            s_logger.LogInformation($"{currency} Storage worker got cancellation request");
        }
        
        private static ICryptoBotPhasesFactory CreateCryptoPhasesFactory(IRepository<CandleStorageObject> candleRepository,
            IRepository<RsiStorageObject> rsiRepository,
            IRepository<MacdStorageObject> macdRepository,
            ISystemClock systemClock,
            ICurrencyClientFactory currencyClientFactory)
        {
            var priceService = new BinancePriceService(currencyClientFactory);
            var priceProvider = new PriceProvider(priceService);
            var candlesProvider = new CandlesProvider(candleRepository);
            var rsiProvider = new RsiProvider(rsiRepository);
            var macdProvider = new MacdProvider(macdRepository);
            var cryptoBotPhasesFactoryCreator = new CryptoBotPhasesFactoryCreator(
                systemClock, 
                priceProvider, 
                rsiProvider, 
                candlesProvider, 
                macdProvider);
            return cryptoBotPhasesFactoryCreator.Create();
        }
    }
}
