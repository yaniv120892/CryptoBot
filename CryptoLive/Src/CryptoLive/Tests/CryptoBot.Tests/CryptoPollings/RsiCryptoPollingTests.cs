using System;
using System.Threading;
using System.Threading.Tasks;
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
    public class RsiCryptoPollingTests
    {
        private static readonly string s_currency = "CurrencyName";
        private static readonly decimal s_maxRsiToNotify = 35;


        private readonly Mock<INotificationService> m_notificationServiceMock = new Mock<INotificationService>();
        private readonly Mock<ICurrencyDataProvider> m_currencyDataProviderMock = new Mock<ICurrencyDataProvider>();
        private readonly ISystemClock m_systemClock = new DummySystemClock();
        
        [TestMethod]
        public async Task When_StartAsync_Given_LowerRsiReached_Should_StopPollingAndReturn()
        {
            // Arrange
            DateTime pollingStartTime = new DateTime(2020, 1, 1, 10, 10, 0);
            const decimal rsiToReturn = 30;

            var expectedResponse = new RsiPollingResponse(pollingStartTime, rsiToReturn);
            m_currencyDataProviderMock
                .Setup(m => m.GetRsi(s_currency, pollingStartTime))
                .Returns(rsiToReturn);

            
            var candleCryptoPolling = new RsiCryptoPolling(m_currencyDataProviderMock.Object,
                m_systemClock,s_maxRsiToNotify);
            
            // Act
            var actualResponse = (RsiPollingResponse)await candleCryptoPolling.StartAsync(s_currency, CancellationToken.None, pollingStartTime);

            // Assert
            Assert.AreEqual(expectedResponse, actualResponse);
            m_currencyDataProviderMock.Verify(
                m=> m.GetRsi(It.IsAny<string>(), It.IsAny<DateTime>()), 
                Times.Once);
        }
        
        [TestMethod]
        public async Task When_StartAsync_Given_LowerRsiReachedOnSecondIteration_Should_StopPollingAndReturn()
        {
            // Arrange
            DateTime pollingStartTime = new DateTime(2020, 1, 1, 10, 10, 0);
            DateTime pollingSecondTime = pollingStartTime.AddMinutes(1);
            const decimal rsiToReturnFirsIteration = 36;
            const decimal rsiToReturnSecondIteration = 30;

            var expectedResponse = new RsiPollingResponse(pollingSecondTime, rsiToReturnSecondIteration);
            m_currencyDataProviderMock
                .Setup(m => m.GetRsi(s_currency, pollingStartTime))
                .Returns(rsiToReturnFirsIteration);
            m_currencyDataProviderMock
                .Setup(m => m.GetRsi(s_currency, pollingSecondTime))
                .Returns(rsiToReturnSecondIteration);

            
            var candleCryptoPolling = new RsiCryptoPolling(m_currencyDataProviderMock.Object,
                m_systemClock,s_maxRsiToNotify);
            
            // Act
            var actualResponse = (RsiPollingResponse)await candleCryptoPolling.StartAsync(s_currency, CancellationToken.None, pollingStartTime);

            // Assert
            Assert.AreEqual(expectedResponse, actualResponse);
            m_currencyDataProviderMock.Verify(
                m=> m.GetRsi(It.IsAny<string>(), It.IsAny<DateTime>()), 
                Times.Exactly(2));
        }

        [TestMethod]
        public async Task When_StartAsync_Given_GotException_Should_PriceAndRsiPollingResponse_Exception_NotEqualNull()
        {
            // Arrange
            Exception exception = new Exception();
            DateTime pollingStartTime = new DateTime(2020, 1, 1, 10, 10, 0);

            var expectedResponse = new RsiPollingResponse(pollingStartTime, -1, false, exception);
            var candleCryptoPolling = new RsiCryptoPolling(m_currencyDataProviderMock.Object,
                m_systemClock,s_maxRsiToNotify);
            
            m_currencyDataProviderMock
                .Setup(m => m.GetRsi(s_currency, pollingStartTime))
                .Throws(exception);
            
            // Act
            var actualResponse = (RsiPollingResponse)await candleCryptoPolling.StartAsync(s_currency, CancellationToken.None, pollingStartTime);

            // Assert
            Assert.AreEqual(expectedResponse, actualResponse);
            m_currencyDataProviderMock.Verify(
                m=> m.GetRsi(It.IsAny<string>(), It.IsAny<DateTime>()), 
                Times.Once);
        }
        
        [TestMethod]
        public async Task When_StartAsync_Given_GotCancellationRequest_Should_PriceAndRsiPollingResponse_IsCancelled_EqualTrue()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            DateTime pollingStartTime = new DateTime(2020, 1, 1, 10, 10, 0);
            const decimal rsiToReturnFirsIteration = 50;

            var expectedResponse = new RsiPollingResponse(pollingStartTime, -1, true);
            var candleCryptoPolling = new RsiCryptoPolling(m_currencyDataProviderMock.Object,
                m_systemClock,s_maxRsiToNotify);
            
            m_currencyDataProviderMock
                .Setup(m => m.GetRsi(s_currency, pollingStartTime))
                .Returns(rsiToReturnFirsIteration).Callback(cancellationTokenSource.Cancel);
            
            // Act
            var actualResponse = (RsiPollingResponse)await candleCryptoPolling.StartAsync(s_currency, cancellationTokenSource.Token, pollingStartTime);

            // Assert
            Assert.AreEqual(expectedResponse, actualResponse);
            m_currencyDataProviderMock.Verify(
                m=> m.GetRsi(It.IsAny<string>(), It.IsAny<DateTime>()), 
                Times.Once);
        }
        
    }
}