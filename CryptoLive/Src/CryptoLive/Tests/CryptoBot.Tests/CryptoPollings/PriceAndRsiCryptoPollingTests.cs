using System;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Abstractions;
using Common.PollingResponses;
using CryptoBot.CryptoPollings;
using Infra;
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

        private readonly Mock<INotificationService> m_notificationServiceMock = new Mock<INotificationService>();
        private readonly Mock<ICurrencyDataProvider> m_currencyDataProviderMock = new Mock<ICurrencyDataProvider>();
        private readonly Mock<ICryptoPriceAndRsiQueue<PriceAndRsi>> m_cryptoPriceAndRsiQueueMock = new Mock<ICryptoPriceAndRsiQueue<PriceAndRsi>>();
        private readonly ISystemClock m_systemClock = new DummySystemClock();
        
        [TestMethod]
        public async Task When_StartAsync_Given_HigherRsiAndLowerPriceReached_Should_StopPollingAndReturn()
        {
            // Arrange
            DateTime pollingStartTime = new DateTime(2020, 1, 1, 10, 10, 0);
            DateTime pollingEndTime = pollingStartTime.AddMinutes(1);
            PriceAndRsi oldPriceAndRsi = new PriceAndRsi(2, 30, pollingStartTime);
            PriceAndRsi newPriceAndRsi = new PriceAndRsi(1, 34, pollingEndTime);

            var expectedResponse = new PriceAndRsiPollingResponse(pollingEndTime, oldPriceAndRsi, newPriceAndRsi);
            m_currencyDataProviderMock
                .Setup(m => m.GetRsiAndClosePrice(s_currency, pollingStartTime))
                .Returns(oldPriceAndRsi);
            m_currencyDataProviderMock
                .Setup(m => m.GetRsiAndClosePrice(s_currency, pollingEndTime))
                .Returns(newPriceAndRsi);

            m_cryptoPriceAndRsiQueueMock.Setup(m => m.GetLowerRsiAndHigherPrice(oldPriceAndRsi)).Returns(default(PriceAndRsi));
            m_cryptoPriceAndRsiQueueMock.Setup(m => m.GetLowerRsiAndHigherPrice(newPriceAndRsi)).Returns(oldPriceAndRsi);
            
            var candleCryptoPolling = new PriceAndRsiCryptoPolling(m_notificationServiceMock.Object,
                m_currencyDataProviderMock.Object,
                m_systemClock, m_cryptoPriceAndRsiQueueMock.Object, s_maxRsiToNotify);
            
            // Act
            PriceAndRsiPollingResponse actualResponse = (PriceAndRsiPollingResponse)await candleCryptoPolling.StartAsync(s_currency, CancellationToken.None, pollingStartTime);

            // Assert
            Assert.AreEqual(expectedResponse, actualResponse);
            m_currencyDataProviderMock.Verify(m=>
                    m.GetRsiAndClosePrice(It.IsAny<string>(), 
                        It.IsAny<DateTime>()),
                Times.Exactly(2));
        }

        [TestMethod]
        public async Task When_StartAsync_Given_GotException_Should_PriceAndRsiPollingResponse_Exception_NotEqualNull()
        {
            // Arrange
            DateTime pollingStartTime = new DateTime(2020, 1, 1, 10, 10, 0);
            DateTime pollingEndTime = pollingStartTime.AddMinutes(1);
            PriceAndRsi priceAndRsi = new PriceAndRsi(2, 50, pollingStartTime);

            Exception exception = new Exception();
            var expectedResponse = new PriceAndRsiPollingResponse(pollingEndTime, null, null, false, exception);
            m_currencyDataProviderMock
                .Setup(m => m.GetRsiAndClosePrice(s_currency, pollingStartTime))
                .Returns(priceAndRsi);
            m_currencyDataProviderMock
                .Setup(m => m.GetRsiAndClosePrice(s_currency, pollingEndTime))
                .Throws(exception);

            var candleCryptoPolling = new PriceAndRsiCryptoPolling(m_notificationServiceMock.Object,
                m_currencyDataProviderMock.Object,
                m_systemClock, m_cryptoPriceAndRsiQueueMock.Object, s_maxRsiToNotify);

            // Act
            PriceAndRsiPollingResponse actualResponse =
                (PriceAndRsiPollingResponse) await candleCryptoPolling.StartAsync(s_currency, CancellationToken.None,
                    pollingStartTime);

            // Assert
            Assert.AreEqual(expectedResponse, actualResponse);
            m_currencyDataProviderMock.Verify(m =>
                    m.GetRsiAndClosePrice(It.IsAny<string>(),
                        It.IsAny<DateTime>()),
                Times.Exactly(2));
            m_cryptoPriceAndRsiQueueMock.Verify(m =>
                    m.Enqueue(It.IsAny<PriceAndRsi>()),
                Times.Once);
        }
        
        [TestMethod]
        public async Task When_StartAsync_Given_GotCancellationRequest_Should_PriceAndRsiPollingResponse_IsCancelled_EqualTrue()
        {
            // Arrange
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            DateTime pollingStartTime = new DateTime(2020, 1, 1, 10, 10, 0);
            PriceAndRsi priceAndRsi = new PriceAndRsi(2, 30, pollingStartTime);

            var expectedResponse = new PriceAndRsiPollingResponse(pollingStartTime, null, null, true);
            m_currencyDataProviderMock
                .Setup(m => m.GetRsiAndClosePrice(s_currency, pollingStartTime))
                .Returns(priceAndRsi).Callback(cancellationTokenSource.Cancel);
            m_cryptoPriceAndRsiQueueMock
                .Setup(m => m.GetLowerRsiAndHigherPrice(priceAndRsi))
                .Returns(default(PriceAndRsi));

            var candleCryptoPolling = new PriceAndRsiCryptoPolling(m_notificationServiceMock.Object,
                m_currencyDataProviderMock.Object,
                m_systemClock, m_cryptoPriceAndRsiQueueMock.Object, s_maxRsiToNotify);
            
            // Act
            PriceAndRsiPollingResponse actualResponse = (PriceAndRsiPollingResponse)await candleCryptoPolling.StartAsync(s_currency, cancellationTokenSource.Token, pollingStartTime);

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
    }
}