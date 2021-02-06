﻿using System;
using System.Collections.Generic;
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
using Storage.Abstractions;
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
        private static readonly DateTime s_initialTime = DateTime.Parse("2/2/2021  12:59:00 PM");

        public static void Main()
        {
            DemoCryptoParameters appParameters = AppParametersLoader<DemoCryptoParameters>.Load(s_configFile);
            s_logger.LogInformation(appParameters.ToString());
            RunMultiplePhases(appParameters).Wait();
        }

        private static async Task RunMultiplePhases(DemoCryptoParameters appParameters)
        {
            var systemClock = new DummySystemClock();
            var candleRepository = new RepositoryImpl<MyCandle>(appParameters.Currencies, deleteOldData: false);
            var rsiRepository = new RepositoryImpl<RsiStorageObject>(appParameters.Currencies, deleteOldData: false);
            var macdRepository = new RepositoryImpl<MacdStorageObject>(appParameters.Currencies, deleteOldData: false);

            DemoCandleService demoCandleService = new DemoCandleService(appParameters.Currencies, 
                appParameters.CandlesDataFolder);

            await RunStorageWorkers(macdRepository, rsiRepository, candleRepository, systemClock, demoCandleService ,
                appParameters.Currencies , appParameters.CandleSize, appParameters.RsiSize, 
                appParameters.FastEmaSize, appParameters.SlowEmaSize, appParameters.SignalSize);
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
            RepositoryImpl<MyCandle> candleRepository,
            ISystemClock systemClock, 
            ICandlesService candlesService , 
            IReadOnlyList<string> currencies, 
            int candleSize,
            int rsiSize,
            int fastEmaSize,
            int slowEmaSize, 
            int signalSize)
        {
            var wsmRepository = new RepositoryImpl<WsmaStorageObject>(currencies, deleteOldData: false);
            var emaAndSignalStorageObject = new RepositoryImpl<EmaAndSignalStorageObject>(currencies, deleteOldData: false);

            var storageWorkersTasks = new Task[currencies.Count];
            for (int i = 0; i < storageWorkersTasks.Length; i++)
            {
                string symbol = currencies[i];
                StorageWorker storageWorker = CreateStorageWorker(rsiRepository, wsmRepository, symbol, rsiSize, macdRepository,
                    emaAndSignalStorageObject, fastEmaSize, slowEmaSize, signalSize, candleRepository, candlesService,
                    systemClock, CancellationToken.None, candleSize);
                storageWorkersTasks[i] = storageWorker.StartAsync(s_initialTime);
            }

            await Task.WhenAll(storageWorkersTasks);
        }

        private static StorageWorker CreateStorageWorker(RepositoryImpl<RsiStorageObject> rsiRepository,
            RepositoryImpl<WsmaStorageObject> wsmRepository,
            string symbol,
            int rsiSize,
            RepositoryImpl<MacdStorageObject> macdRepository,
            RepositoryImpl<EmaAndSignalStorageObject> emaAndSignalStorageObject,
            int fastEmaSize,
            int slowEmaSize,
            int signalSize, 
            RepositoryImpl<MyCandle> candleRepository, 
            ICandlesService candlesService, 
            ISystemClock systemClock, 
            CancellationToken cancellationToken, 
            int candleSize)
        {
            IRepositoryUpdater rsiRepositoryUpdater = new RsiRepositoryUpdater(rsiRepository, wsmRepository, symbol, rsiSize);
            IRepositoryUpdater macdRepositoryUpdater = new MacdRepositoryUpdater(macdRepository, emaAndSignalStorageObject,
                symbol,
                fastEmaSize, slowEmaSize, signalSize);
            IRepositoryUpdater candleRepositoryUpdater = new CandleRepositoryUpdater(candleRepository, symbol);
            var storageWorker = new StorageWorker(candlesService,
                systemClock,
                rsiRepositoryUpdater,
                candleRepositoryUpdater,
                macdRepositoryUpdater,
                cancellationToken,
                candleSize,
                symbol,
                StorageWorkerMode.Demo);
            return storageWorker;        
        }

        private static ICryptoBotPhasesFactory CreateCryptoPhasesFactory(RepositoryImpl<MyCandle> candleRepository,
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
            DateTime currentTime = s_initialTime;
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
