using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Abstractions;
using Common.PollingResponses;
using CryptoBot.CryptoPollings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Storage.Abstractions.Providers;
using Utils.Abstractions;
using Utils.SystemClocks;

namespace CryptoBot.Tests.CryptoPollings
{
    [TestClass]
    public class PriceAndRsiCryptoPollingTests
    {
        private static readonly string s_currency = "CurrencyName";
        private static readonly decimal s_maxRsiToNotify = 35;
        private static readonly int s_candleSize = 15;
        private static readonly DateTime s_pollingStartTime = new DateTime(2020, 1, 1, 10, 10, 0);
        private static readonly DateTime s_pollingEndTime = s_pollingStartTime.AddMinutes(1);


        private readonly Mock<ICurrencyDataProvider> m_currencyDataProviderMock = new Mock<ICurrencyDataProvider>();
        private readonly Mock<ICryptoPriceAndRsiQueue<PriceAndRsi>> m_cryptoPriceAndRsiQueueMock = new Mock<ICryptoPriceAndRsiQueue<PriceAndRsi>>();
        private readonly ISystemClock m_systemClock = new DummySystemClock();

        [TestMethod]
        public async Task When_StartAsync_Given_HigherRsiAndLowerPriceReached_Should_StopPollingAndReturn()
        {
            // Arrange
            var oldPriceAndRsi = new PriceAndRsi(2, 30, s_pollingStartTime);
            var newPriceAndRsi = new PriceAndRsi(1, 34, s_pollingEndTime);
            var expectedResponse = new PriceAndRsiPollingResponse(s_pollingEndTime, oldPriceAndRsi, newPriceAndRsi);
            
            m_currencyDataProviderMock
                .Setup(m => m.GetRsiAndClosePrice(s_currency, s_pollingStartTime))
                .Returns(oldPriceAndRsi);
            m_currencyDataProviderMock
                .Setup(m => m.GetRsiAndClosePrice(s_currency, s_pollingEndTime))
                .Returns(newPriceAndRsi);
            m_cryptoPriceAndRsiQueueMock
                .Setup(m => m.GetLowerRsiAndHigherPrice(oldPriceAndRsi))
                .Returns(default(PriceAndRsi));
            m_cryptoPriceAndRsiQueueMock.
                Setup(m => m.GetLowerRsiAndHigherPrice(newPriceAndRsi))
                .Returns(oldPriceAndRsi);
            
            PriceAndRsiCryptoPolling sut = CreatePriceAndRsiCryptoPolling();
            
            // Act
            PriceAndRsiPollingResponse actualResponse = (PriceAndRsiPollingResponse)await sut.StartAsync(s_currency, CancellationToken.None, s_pollingStartTime);

            // Assert
            Assert.AreEqual(expectedResponse, actualResponse);
            m_currencyDataProviderMock.Verify(m=>
                    m.GetRsiAndClosePrice(It.IsAny<string>(), It.IsAny<DateTime>()),
                Times.Exactly(2));
        }

        [TestMethod]
        public async Task When_StartAsync_Given_GotException_Should_PriceAndRsiPollingResponse_Exception_NotEqualNull()
        {
            // Arrange
            var priceAndRsi = new PriceAndRsi(2, 50, s_pollingStartTime);
            var exception = new Exception();
            const bool isCancelled = false;
            var expectedResponse = new PriceAndRsiPollingResponse(s_pollingEndTime, null, null,
                isCancelled, exception);
            
            m_currencyDataProviderMock
                .Setup(m => m.GetRsiAndClosePrice(s_currency, s_pollingStartTime))
                .Returns(priceAndRsi);
            m_currencyDataProviderMock
                .Setup(m => m.GetRsiAndClosePrice(s_currency, s_pollingEndTime))
                .Throws(exception);

            PriceAndRsiCryptoPolling sut = CreatePriceAndRsiCryptoPolling();

            // Act
            PriceAndRsiPollingResponse actualResponse =
                (PriceAndRsiPollingResponse) await sut.StartAsync(s_currency, CancellationToken.None,
                    s_pollingStartTime);

            // Assert
            Assert.AreEqual(expectedResponse, actualResponse);
            m_currencyDataProviderMock.Verify(m =>
                    m.GetRsiAndClosePrice(It.IsAny<string>(), It.IsAny<DateTime>()),
                Times.Exactly(2));
            m_cryptoPriceAndRsiQueueMock.Verify(m => 
                    m.Enqueue(It.IsAny<PriceAndRsi>()),
                Times.Once);
        }
        
        [TestMethod]
        public async Task When_StartAsync_Given_GotCancellationRequest_Should_PriceAndRsiPollingResponse_IsCancelled_EqualTrue()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            var priceAndRsi = new PriceAndRsi(2, 30, s_pollingStartTime);
            var expectedResponse = new PriceAndRsiPollingResponse(s_pollingStartTime, null, null, true);
            
            m_currencyDataProviderMock
                .Setup(m => m.GetRsiAndClosePrice(s_currency, s_pollingStartTime))
                .Returns(priceAndRsi).Callback(cancellationTokenSource.Cancel);
            m_cryptoPriceAndRsiQueueMock
                .Setup(m => m.GetLowerRsiAndHigherPrice(priceAndRsi))
                .Returns(default(PriceAndRsi));

            PriceAndRsiCryptoPolling sut = CreatePriceAndRsiCryptoPolling();

            // Act
            PriceAndRsiPollingResponse actualResponse = (PriceAndRsiPollingResponse)await sut.StartAsync(s_currency, cancellationTokenSource.Token, s_pollingStartTime);

            // Assert
            Assert.AreEqual(expectedResponse, actualResponse);
            m_currencyDataProviderMock.Verify(m=>
                    m.GetRsiAndClosePrice(It.IsAny<string>(), 
                        It.IsAny<DateTime>()),
                Times.Once);
            m_cryptoPriceAndRsiQueueMock.Verify(m=>
                    m.Enqueue(It.IsAny<PriceAndRsi>()),
                Times.Once);
        }
        
        [TestMethod]
        public async Task When_StartAsync_Given_HasRunningParent_AndCandleSizeIs_10_Should_Run_9_Cycles_AndWaitForParentToFinish()
        {
            // Arrange
            const int candleSize = 10;
            const int expectedPollingIterations = candleSize - 1;
            var oldPriceAndRsi = new PriceAndRsi(2, 30, s_pollingStartTime);
            DateTime pollingEndTime = s_pollingStartTime.AddMinutes(expectedPollingIterations);
            var newPriceAndRsi = new PriceAndRsi(1, 34, pollingEndTime);
            var expectedResponse = new PriceAndRsiPollingResponse(pollingEndTime, oldPriceAndRsi, newPriceAndRsi);
            var cancellationTokenSource = new CancellationTokenSource();
            var parentIsRunningCancellationTokenSource = new CancellationTokenSource();

            int i = 0;
            while (i < expectedPollingIterations)
            {
                DateTime currentPollingTime = s_pollingStartTime.AddMinutes(i);
                PriceAndRsi currentPriceAndRsi = new PriceAndRsi(2, 30, currentPollingTime);
                m_currencyDataProviderMock
                    .Setup(m => m.GetRsiAndClosePrice(s_currency, currentPollingTime))
                    .Returns(currentPriceAndRsi);
                i++;
            }
            
            m_currencyDataProviderMock
                .Setup(m => m.GetRsiAndClosePrice(s_currency, pollingEndTime))
                .Returns(newPriceAndRsi);
            m_cryptoPriceAndRsiQueueMock
                .Setup(m => m.GetLowerRsiAndHigherPrice(newPriceAndRsi))
                .Returns(oldPriceAndRsi);

            var parentRunningCancellationToken = new Queue<CancellationToken>();
            parentRunningCancellationToken.Enqueue(parentIsRunningCancellationTokenSource.Token);
            PriceAndRsiCryptoPolling sut = CreatePriceAndRsiCryptoPolling(parentRunningCancellationToken, candleSize);

            // Act
            Task<PollingResponseBase> task = sut.StartAsync(s_currency, cancellationTokenSource.Token, s_pollingStartTime);

            // Assert
            // wait for PriceAndRsiPolling get to point it needs to wait for parent to finish
            await Task.Delay(1000);
            m_currencyDataProviderMock.Verify(m=>
                    m.GetRsiAndClosePrice(It.IsAny<string>(), 
                        It.IsAny<DateTime>()),
                Times.Exactly(expectedPollingIterations));
            m_cryptoPriceAndRsiQueueMock.Verify(m=>
                    m.GetLowerRsiAndHigherPrice(It.IsAny<PriceAndRsi>()),
                Times.Exactly(expectedPollingIterations));
            m_cryptoPriceAndRsiQueueMock.Verify(m=>
                    m.Enqueue(It.IsAny<PriceAndRsi>()),
                Times.Exactly(expectedPollingIterations));
            
            // Release children
            parentIsRunningCancellationTokenSource.Cancel();
            
            PriceAndRsiPollingResponse actualResponse = (PriceAndRsiPollingResponse) await task;
            Assert.AreEqual(expectedResponse, actualResponse);
            m_currencyDataProviderMock.Verify(m=>
                    m.GetRsiAndClosePrice(It.IsAny<string>(), 
                        It.IsAny<DateTime>()),
                Times.Exactly(expectedPollingIterations+1));
            m_cryptoPriceAndRsiQueueMock.Verify(m=>
                    m.GetLowerRsiAndHigherPrice(It.IsAny<PriceAndRsi>()),
                Times.Exactly(expectedPollingIterations+1));
            m_cryptoPriceAndRsiQueueMock.Verify(m=>
                    m.Enqueue(It.IsAny<PriceAndRsi>()),
                Times.Exactly(expectedPollingIterations));
        }

        private PriceAndRsiCryptoPolling CreatePriceAndRsiCryptoPolling(Queue<CancellationToken> parentRunningCancellationToken, 
            int iterationUntilWaitForParentCancellationToken ) =>
            new PriceAndRsiCryptoPolling(m_currencyDataProviderMock.Object,
                m_systemClock, 
                m_cryptoPriceAndRsiQueueMock.Object, 
                s_maxRsiToNotify,
                parentRunningCancellationToken,
                iterationUntilWaitForParentCancellationToken);

        private PriceAndRsiCryptoPolling CreatePriceAndRsiCryptoPolling() =>
            CreatePriceAndRsiCryptoPolling(new Queue<CancellationToken>(),
                s_candleSize);
    }
}