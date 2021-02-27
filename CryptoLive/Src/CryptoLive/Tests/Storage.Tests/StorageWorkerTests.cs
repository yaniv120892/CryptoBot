using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.DataStorageObjects;
using Infra;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Services.Abstractions;
using Storage.Providers;
using Storage.Repository;
using Storage.Updaters;
using Storage.Workers;
using Utils.Abstractions;
using Utils.Calculators;

namespace Storage.Tests
{
    [TestClass]
    public class StorageWorkerTests
    {
        private static readonly string s_currency = "CurrencyName";
        private static readonly string s_calculatedDataFolder = String.Empty;
        private static readonly int s_candleSize = 2;
        private static readonly int s_rsiSize = 5;
        private static readonly int s_fastEmaSize = 6;
        private static readonly int s_slowEmaSize = 11;
        private static readonly int s_signalSize = 7;
        private static readonly Dictionary<string, string> s_currenciesToCalculatedDataFiles =
            new Dictionary<string, string> 
            {{s_currency,String.Empty}};
        private static readonly DateTime s_storageWorkerStartTime = new DateTime(2020,1,1,10,0,0);

        [TestMethod]
        public async Task When_StartAsync_Given_AddDataToRepositories_TookLongTime_Should_WorkerStatus_Equal_Faulted()
        {
            // Arrange
            const decimal candleOpen = 1;
            const decimal candleClose = 2;
            const decimal candleLow = (decimal)0.8;
            const decimal candleHigh = (decimal)2.5;
            DateTime candleCloseTime = s_storageWorkerStartTime.Subtract(TimeSpan.FromSeconds(1));
            DateTime candleOpenTime = candleCloseTime.Subtract(TimeSpan.FromSeconds(59)).Subtract(TimeSpan.FromMinutes(s_candleSize-1));
            MyCandle expectedCandle = new MyCandle(candleOpen, candleClose, candleOpenTime, candleCloseTime, candleLow,
                candleHigh);
            decimal expectedRsi = RsiCalculator.Calculate(candleClose-candleOpen,candleClose-candleOpen);
            //decimal expectedMacd = MacdHistogramCalculator.Calculate(candleClose, candleClose, 0);

            var cancellationTokenSource = new CancellationTokenSource();
            var candlesServiceMock = new Mock<ICandlesService>();
            var systemClockMock = new Mock<ISystemClock>();
            var stopWatchMock = new Mock<IStopWatch>();
            var notificationServiceMock = new Mock<INotificationService>();
            
            var rsiRepository = new RepositoryImpl<RsiStorageObject>(s_currenciesToCalculatedDataFiles, false);
            var wsmaRepository = new RepositoryImpl<WsmaStorageObject>(s_currenciesToCalculatedDataFiles, false);
            var rsiRepositoryUpdater = new RsiRepositoryUpdater(rsiRepository, wsmaRepository, s_currency, s_rsiSize, s_calculatedDataFolder);
            var rsiProvider = new RsiProvider(rsiRepository);
            
            var candleRepository = new RepositoryImpl<CandleStorageObject>(s_currenciesToCalculatedDataFiles, false);
            var candleRepositoryUpdater = new CandleRepositoryUpdater(candleRepository, s_currency, s_candleSize, s_calculatedDataFolder);
            var candleProvider = new CandlesProvider(candleRepository);

            var macdRepository = new RepositoryImpl<MacdStorageObject>(s_currenciesToCalculatedDataFiles, false);
            var emaAndSignalStorageObject = new RepositoryImpl<EmaAndSignalStorageObject>(s_currenciesToCalculatedDataFiles, false);
            var macdRepositoryUpdater = new MacdRepositoryUpdater(macdRepository, emaAndSignalStorageObject, s_currency,
                s_fastEmaSize, s_slowEmaSize, s_signalSize, s_calculatedDataFolder);
            //var macdProvider = new MacdProvider(macdRepository);

            var sut = new StorageWorker(notificationServiceMock.Object,
                candlesServiceMock.Object,
                systemClockMock.Object,
                stopWatchMock.Object,
                rsiRepositoryUpdater,
                candleRepositoryUpdater,
                macdRepositoryUpdater,
                cancellationTokenSource.Token,
                s_candleSize,
                s_currency,
                false,
                60);
            stopWatchMock.Setup(m => m.ElapsedSeconds)
                .Returns(120);

            var candleToReturn0 = new MyCandle(1, 2,
                s_storageWorkerStartTime.Subtract(TimeSpan.FromMinutes(2)), 
                s_storageWorkerStartTime.Subtract(TimeSpan.FromMinutes(2)).AddSeconds(59), 
                1, 2);
            var candleToReturn1 = new MyCandle(1, 2, 
                s_storageWorkerStartTime.Subtract(TimeSpan.FromMinutes(1)), 
                s_storageWorkerStartTime.Subtract(TimeSpan.FromMinutes(1)).AddSeconds(59), 
                (decimal)0.8, (decimal)2.5);
            var candleToReturn2 = new MyCandle(1, 2, 
                s_storageWorkerStartTime, 
                s_storageWorkerStartTime.AddSeconds(59), 
                1, 2);
            Memory<MyCandle> oneMinuteCandlesToReturn = new MyCandle[3];
            oneMinuteCandlesToReturn.Span[0] = candleToReturn0;
            oneMinuteCandlesToReturn.Span[1] = candleToReturn1;
            oneMinuteCandlesToReturn.Span[2] = candleToReturn2;
            candlesServiceMock.Setup(m => m.GetOneMinuteCandles(s_currency, 3, s_storageWorkerStartTime))
                .Returns(Task.FromResult(oneMinuteCandlesToReturn));

            systemClockMock.Setup(m => m.Wait(cancellationTokenSource.Token, s_currency, 60, It.IsAny<string>(),
                    s_storageWorkerStartTime))
                .Returns(Task.FromResult(s_storageWorkerStartTime.AddMinutes(2)));
            
            // Act
            await sut.StartAsync(s_storageWorkerStartTime);
            
            // Assert
            Assert.AreEqual(expectedCandle, candleProvider.GetLastCandle(s_currency, candleCloseTime));
            Assert.AreEqual(expectedRsi, rsiProvider.Get(s_currency, candleCloseTime));
            //Assert.AreEqual(expectedMacd,macdProvider.Get(s_currency, candleCloseTime));
            Assert.AreEqual(WorkerStatus.Faulted, sut.WorkerStatus);
        }

        [TestMethod]
        public async Task When_StartAsync_Given_SendCancellationRequest_Should_WorkerStatus_Equal_Cancelled()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            var candlesServiceMock = new Mock<ICandlesService>();
            var systemClockMock = new Mock<ISystemClock>();
            var stopWatchMock = new Mock<IStopWatch>();
            var notificationServiceMock = new Mock<INotificationService>();

            var rsiRepository = new RepositoryImpl<RsiStorageObject>(s_currenciesToCalculatedDataFiles, false);
            var wsmaRepository = new RepositoryImpl<WsmaStorageObject>(s_currenciesToCalculatedDataFiles, false);
            var rsiRepositoryUpdater = new RsiRepositoryUpdater(rsiRepository, wsmaRepository, s_currency, s_rsiSize, s_calculatedDataFolder);
            
            var candleRepository = new RepositoryImpl<CandleStorageObject>(s_currenciesToCalculatedDataFiles, false);
            var candleRepositoryUpdater = new CandleRepositoryUpdater(candleRepository, s_currency, s_candleSize, s_calculatedDataFolder);

            var macdRepository = new RepositoryImpl<MacdStorageObject>(s_currenciesToCalculatedDataFiles, false);
            var emaAndSignalStorageObject = new RepositoryImpl<EmaAndSignalStorageObject>(s_currenciesToCalculatedDataFiles, false);
            var macdRepositoryUpdater = new MacdRepositoryUpdater(macdRepository, emaAndSignalStorageObject, s_currency,
                s_fastEmaSize, s_slowEmaSize, s_signalSize, s_calculatedDataFolder);

            var sut = new StorageWorker(notificationServiceMock.Object,
                candlesServiceMock.Object,
                systemClockMock.Object,
                stopWatchMock.Object,
                rsiRepositoryUpdater,
                candleRepositoryUpdater,
                macdRepositoryUpdater,
                cancellationTokenSource.Token,
                s_candleSize,
                s_currency,
                false,
                60);

            // Act
            cancellationTokenSource.Cancel();
            await sut.StartAsync(s_storageWorkerStartTime);
            
            // Assert
            Assert.AreEqual(WorkerStatus.Cancelled, sut.WorkerStatus);
        }
    }
}