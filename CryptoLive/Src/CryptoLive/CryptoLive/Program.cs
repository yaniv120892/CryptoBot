using System;
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
using Services;
using Services.Abstractions;
using Storage.Abstractions;
using Storage.Providers;
using Storage.Repository;
using Storage.Updaters;
using Storage.Workers;
using Utils.Abstractions;
using Utils.SystemClocks;

namespace CryptoLive
{
    public class Program
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<Program>();
        private static readonly string s_configFile = "appsettings.json";
        private static readonly int s_botVersion = 1;

        public static void Main()
        {
            CryptoLiveParameters appParameters = AppParametersLoader<CryptoLiveParameters>.Load(s_configFile);
            s_logger.LogInformation(appParameters.ToString());
            RunMultiplePhases(appParameters).Wait();
        }

        private static async Task RunMultiplePhases(CryptoLiveParameters cryptoLiveParameters)
        {               
            var systemClock = new SystemClock();
            var systemClockWithDelay = new SystemClockWithDelay(systemClock ,cryptoLiveParameters.BotDelayTime);
            var cancellationTokenSource = new CancellationTokenSource();
            
            var currencyClientFactory = new CurrencyClientFactory(cryptoLiveParameters.BinanceApiKey, cryptoLiveParameters.BinanceApiSecretKey);
            var candlesService = new BinanceCandleService(currencyClientFactory);

            var candleRepository = new RepositoryImpl<MyCandle>(cryptoLiveParameters.Currencies);
            var rsiRepository = new RepositoryImpl<RsiStorageObject>(cryptoLiveParameters.Currencies);
            var wsmRepository = new RepositoryImpl<WsmaStorageObject>(cryptoLiveParameters.Currencies);
            var macdRepository = new RepositoryImpl<MacdStorageObject>(cryptoLiveParameters.Currencies);
            var emaAndSignalStorageObject = new RepositoryImpl<EmaAndSignalStorageObject>(cryptoLiveParameters.Currencies);
            
            int rsiSize = cryptoLiveParameters.RsiSize;
            int fastEmaSize = cryptoLiveParameters.FastEmaSize;
            int slowEmaSize = cryptoLiveParameters.SlowEmaSize;
            int signalSize = cryptoLiveParameters.SignalSize;
            int candleSize = cryptoLiveParameters.CandleSize;
            
            ICryptoBotPhasesFactory cryptoBotPhasesFactory = CreateCryptoPhasesFactory(candleRepository, rsiRepository, 
                macdRepository, systemClockWithDelay, currencyClientFactory);
            
            var currencyBotTasks = new Task[cryptoLiveParameters.Currencies.Length];
            var storageWorkersTasks = new Task[cryptoLiveParameters.Currencies.Length];
            
            for (int i = 0; i < currencyBotTasks.Length; i++)
            {
                string symbol = cryptoLiveParameters.Currencies[i];
                StorageWorker storageWorker = CreateStorageWorker(rsiRepository, wsmRepository, symbol, rsiSize, macdRepository,
                    emaAndSignalStorageObject, fastEmaSize, slowEmaSize, signalSize, candleRepository, candlesService,
                    systemClock, cancellationTokenSource, candleSize);
                CurrencyBot currencyBot = CurrencyBotFactory.Create(cryptoLiveParameters, cryptoBotPhasesFactory, symbol);
                DateTime storageStartTime = await systemClock.Wait(CancellationToken.None, symbol, 0, "Init",DateTime.UtcNow);
                storageWorkersTasks[i] = storageWorker.StartAsync(storageStartTime);
                await systemClock.Wait(CancellationToken.None, symbol, 120, "Init2",storageStartTime);
                currencyBotTasks[i] = RunMultiplePhasesPerCurrency(currencyBot, storageStartTime);
            }

            await Task.WhenAll(currencyBotTasks);
        }

        private static StorageWorker CreateStorageWorker(RepositoryImpl<RsiStorageObject> rsiRepository, RepositoryImpl<WsmaStorageObject> wsmRepository,
            string symbol, int rsiSize, RepositoryImpl<MacdStorageObject> macdRepository, RepositoryImpl<EmaAndSignalStorageObject> emaAndSignalStorageObject,
            int fastEmaSize, int slowEmaSize, int signalSize, RepositoryImpl<MyCandle> candleRepository,
            BinanceCandleService candlesService, SystemClock systemClock, CancellationTokenSource cancellationTokenSource,
            int candleSize)
        {
            var rsiRepositoryUpdater = new RsiRepositoryUpdater(rsiRepository, wsmRepository, symbol, rsiSize);
            var macdRepositoryUpdater = new MacdRepositoryUpdater(macdRepository, emaAndSignalStorageObject,
                symbol, fastEmaSize, slowEmaSize, signalSize);
            var candleRepositoryUpdater = new CandleRepositoryUpdater(candleRepository, symbol);
            
            var storageWorker = new StorageWorker(candlesService,
                systemClock,
                rsiRepositoryUpdater,
                candleRepositoryUpdater,
                macdRepositoryUpdater,
                cancellationTokenSource.Token,
                candleSize,
                symbol,
                StorageWorkerMode.Live);
            return storageWorker;
        }

        private static async Task RunMultiplePhasesPerCurrency(CurrencyBot currencyBot, DateTime timeToStartBot)
        {
            int gainCounter = 0;
            int lossCounter = 0;
            int noChangeCounter = 0;
            while(gainCounter + lossCounter < 10)
            {
                (BotResult botResult, DateTime _) = await currencyBot.StartAsync(timeToStartBot, s_botVersion);
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

            s_logger.LogInformation($"{currencyBot.Currency}: Gain - {gainCounter}, Loss - {lossCounter}, NoChange: {noChangeCounter}");
        }
        
        private static ICryptoBotPhasesFactory CreateCryptoPhasesFactory(IRepository<MyCandle> candleRepository,
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
            var notificationService = new EmptyNotificationService();
            var cryptoBotPhasesFactoryCreator = new CryptoBotPhasesFactoryCreator(
                systemClock, 
                priceProvider, 
                rsiProvider, 
                candlesProvider, 
                macdProvider,
                notificationService);
            return cryptoBotPhasesFactoryCreator.Create();
        }
    }
}
