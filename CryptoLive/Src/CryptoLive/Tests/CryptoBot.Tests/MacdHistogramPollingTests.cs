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

namespace CryptoBot.Tests
{
    [TestClass]
    public class MacdHistogramPollingTests
    {
        private static readonly string s_currency = "CurrencyName";
        private static readonly int s_candleSize = 15;
        
        private readonly Mock<INotificationService> m_notificationServiceMock = new Mock<INotificationService>();
        private readonly Mock<ICurrencyDataProvider> m_currencyDataProviderMock = new Mock<ICurrencyDataProvider>();
        private readonly ISystemClock m_systemClock = new DummySystemClock();
        
        [TestMethod]
        public async Task When_StartAsync_Given_MacdReachedPositiveValue_Return_PositiveMacdValue()
        {
            // Arrange
            DateTime pollingStartTime = new DateTime(2020, 1, 1, 10, 10, 0);
            DateTime pollingEndTime = pollingStartTime.AddMinutes(1);
            const decimal positiveMacdHistogram = 1;
            const int maxMacdPollingTimeInMinutes = 1;
            var expectedResponse = new MacdHistogramPollingResponse(pollingEndTime, positiveMacdHistogram);
            m_currencyDataProviderMock
                .Setup(m => m.GetMacdHistogram(s_currency, s_candleSize, pollingEndTime))
                .Returns(positiveMacdHistogram);
            
            var macdHistogramCryptoPolling = new MacdHistogramCryptoPolling(m_notificationServiceMock.Object,
                m_currencyDataProviderMock.Object,
                m_systemClock, 
                s_candleSize,
                maxMacdPollingTimeInMinutes);
            
            // Act
            MacdHistogramPollingResponse actualResponse = (MacdHistogramPollingResponse)await macdHistogramCryptoPolling.StartAsync(s_currency, CancellationToken.None, pollingStartTime);

            // Assert
            Assert.AreEqual(expectedResponse, actualResponse);
            m_currencyDataProviderMock.Verify(m=>
                    m.GetMacdHistogram(It.IsAny<string>(), 
                        It.IsAny<int>(), 
                        It.IsAny<DateTime>()),
                Times.Once);
        }
        
        [TestMethod]
        public async Task When_StartAsync_Given_MacdNotReachedPositiveValue_Return_IsReachMaxTimeInMinutesEqualsTrue()
        {
            // Arrange
            DateTime pollingStartTime = new DateTime(2020, 1, 1, 10, 10, 0);
            DateTime pollingEndTime = pollingStartTime.AddMinutes(1);
            const decimal negativeMacdHistogram = -1;
            const int maxMacdPollingTimeInMinutes = 1;
            var expectedResponse = new MacdHistogramPollingResponse(pollingEndTime, 0, true);
            m_currencyDataProviderMock
                .Setup(m => m.GetMacdHistogram(s_currency, s_candleSize, pollingEndTime))
                .Returns(negativeMacdHistogram);
            
            var macdHistogramCryptoPolling = new MacdHistogramCryptoPolling(m_notificationServiceMock.Object,
                m_currencyDataProviderMock.Object,
                m_systemClock, 
                s_candleSize,
                maxMacdPollingTimeInMinutes);
            
            // Act
            MacdHistogramPollingResponse actualResponse = (MacdHistogramPollingResponse)await macdHistogramCryptoPolling.StartAsync(s_currency, CancellationToken.None, pollingStartTime);

            // Assert
            Assert.AreEqual(expectedResponse, actualResponse);
            m_currencyDataProviderMock.Verify(m=>
                    m.GetMacdHistogram(It.IsAny<string>(), 
                        It.IsAny<int>(), 
                        It.IsAny<DateTime>()),
                Times.Once);
        }
        
        [TestMethod]
        public async Task When_StartAsync_Given_MacdReachedPositiveValueInSecondIteration_Return_PositiveMacdValue()
        {
            // Arrange
            DateTime pollingStartTime = new DateTime(2020, 1, 1, 10, 10, 0);
            DateTime getMacdHistogramFirstTime = pollingStartTime.AddMinutes(1);
            DateTime getMacdHistogramSecondTime = getMacdHistogramFirstTime.AddMinutes(1);
            const decimal positiveMacdHistogram = 1;
            const decimal negativeMacdHistogram = -1;
            const int maxMacdPollingTimeInMinutes = 2;
            var expectedResponse = new MacdHistogramPollingResponse(getMacdHistogramSecondTime, positiveMacdHistogram);
            m_currencyDataProviderMock
                .Setup(m => m.GetMacdHistogram(s_currency, s_candleSize, getMacdHistogramFirstTime))
                .Returns(negativeMacdHistogram);
            m_currencyDataProviderMock
                .Setup(m => m.GetMacdHistogram(s_currency, s_candleSize, getMacdHistogramSecondTime))
                .Returns(positiveMacdHistogram);
            
            var macdHistogramCryptoPolling = new MacdHistogramCryptoPolling(m_notificationServiceMock.Object,
                m_currencyDataProviderMock.Object,
                m_systemClock, 
                s_candleSize,
                maxMacdPollingTimeInMinutes);
            
            // Act
            MacdHistogramPollingResponse actualResponse = (MacdHistogramPollingResponse)await macdHistogramCryptoPolling.StartAsync(s_currency, CancellationToken.None, pollingStartTime);

            // Assert
            Assert.AreEqual(expectedResponse, actualResponse);
            m_currencyDataProviderMock.Verify(m=>
                    m.GetMacdHistogram(It.IsAny<string>(), 
                        It.IsAny<int>(), 
                        It.IsAny<DateTime>()),
                Times.Exactly(2));
        }

        [TestMethod]
        public async Task When_StartAsync_Given_GotCancellationRequest_Should_PollingResponse_IsCancelled_EqualsTrue()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            DateTime pollingStartTime = new DateTime(2020, 1, 1, 10, 10, 0);
            DateTime pollingEndTime = pollingStartTime.AddMinutes(1);
            const decimal negativeMacdHistogram = -1;
            const int maxMacdPollingTimeInMinutes = 2;
            m_currencyDataProviderMock
                .Setup(m => m.GetMacdHistogram(s_currency, s_candleSize, pollingEndTime))
                .Returns(negativeMacdHistogram)
                .Callback(()=>cancellationTokenSource.Cancel());

            var macdHistogramCryptoPolling = new MacdHistogramCryptoPolling(m_notificationServiceMock.Object,
                m_currencyDataProviderMock.Object,
                m_systemClock, 
                s_candleSize,
                maxMacdPollingTimeInMinutes);
            
            // Act
            MacdHistogramPollingResponse actualResponse = (MacdHistogramPollingResponse)await macdHistogramCryptoPolling.StartAsync(s_currency, cancellationTokenSource.Token, pollingStartTime);

            // Assert
            Assert.IsTrue(actualResponse.IsCancelled);
            Assert.IsNull(actualResponse.Exception);
            Assert.IsFalse(actualResponse.IsReachMaxTimeInMinutes);
            Assert.AreEqual(pollingEndTime, actualResponse.Time);
            m_currencyDataProviderMock.Verify(m=>
                    m.GetMacdHistogram(It.IsAny<string>(), 
                        It.IsAny<int>(), 
                        It.IsAny<DateTime>()),
                Times.Once);
        }

        [TestMethod]
        public async Task When_StartAsync_Given_GotException_Should_CandlePollingResponse_Exception_NotEqualNull()
        {
            // Arrange
            Exception expectedException = new Exception();
            DateTime pollingStartTime = new DateTime(2020, 1, 1, 10, 10, 0);
            DateTime pollingEndTime = pollingStartTime.AddMinutes(1);
            const int maxMacdPollingTimeInMinutes = 1;
            m_currencyDataProviderMock
                .Setup(m => m.GetMacdHistogram(s_currency, s_candleSize, pollingEndTime))
                .Throws(expectedException);


            var macdHistogramCryptoPolling = new MacdHistogramCryptoPolling(m_notificationServiceMock.Object,
                m_currencyDataProviderMock.Object,
                m_systemClock,
                s_candleSize,
                maxMacdPollingTimeInMinutes);

            // Act
            MacdHistogramPollingResponse actualResponse =
                (MacdHistogramPollingResponse) await macdHistogramCryptoPolling.StartAsync(s_currency,
                    CancellationToken.None, pollingStartTime);

            // Assert
            Assert.AreEqual(expectedException, actualResponse.Exception);
            Assert.IsFalse(actualResponse.IsCancelled);
            Assert.IsFalse(actualResponse.IsReachMaxTimeInMinutes);
            Assert.AreEqual(pollingEndTime, actualResponse.Time);
            m_currencyDataProviderMock.Verify(m =>
                    m.GetMacdHistogram(It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<DateTime>()),
                Times.Once);
        }
    }
}