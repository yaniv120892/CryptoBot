using System;
using System.Threading;
using System.Threading.Tasks;
using Common;
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
    public class CandleCryptoPollingTests
    {
        private static readonly string s_currency = "CurrencyName";
        private static readonly int s_candleSize = 15;
        private static readonly int s_delayTimeInSeconds = s_candleSize * 60;
        private static readonly decimal s_minPrice = 100;
        private static readonly decimal s_maxPrice = 200;
        private static readonly decimal s_higherThanMinPrice = s_minPrice + 1;
        private static readonly decimal s_higherThanMaxPrice = s_maxPrice + 1;
        private static readonly decimal s_lowerThanMinPrice = s_minPrice - 1;
        private static readonly decimal s_lowerThanMaxPrice = s_maxPrice - 1;
        
        private readonly Mock<ICurrencyDataProvider> m_currencyDataProviderMock = new Mock<ICurrencyDataProvider>();
        private readonly ISystemClock m_systemClock = new DummySystemClock();
        
        [TestMethod]
        public async Task When_StartAsync_Given_CandleReachHighPrice_Return_IsAboveTrue()
        {
            // Arrange
            MyCandle candle = CreateCandle(s_higherThanMinPrice, s_higherThanMaxPrice);
            DateTime pollingStartTime = candle.CloseTime;
            var expectedCandlePollingResponse = new CandlePollingResponse(false, true, candle.CloseTime, candle);
            m_currencyDataProviderMock
                .Setup(m => m.GetLastCandle(s_currency, pollingStartTime))
                .Returns(candle);
            
            var candleCryptoPolling = new CandleCryptoPolling(m_currencyDataProviderMock.Object,
                m_systemClock, s_delayTimeInSeconds,
                s_candleSize,
                s_minPrice,
                s_maxPrice);
            
            // Act
            CandlePollingResponse actualCandlePollingResponse = (CandlePollingResponse)await candleCryptoPolling.StartAsync(s_currency, CancellationToken.None, pollingStartTime);

            // Assert
            Assert.AreEqual(expectedCandlePollingResponse, actualCandlePollingResponse);
            m_currencyDataProviderMock.Verify(m=>
                    m.GetLastCandle(It.IsAny<string>(), 
                        It.IsAny<DateTime>()),
                Times.Once);
        }
        
        [TestMethod]
        public async Task When_StartAsync_Given_CandleReachLowerPrice_Return_IsBelowTrue()
        {
            // Arrange
            MyCandle candle = CreateCandle(s_lowerThanMinPrice, s_lowerThanMaxPrice);
            DateTime pollingStartTime = candle.CloseTime;
            var expectedCandlePollingResponse = new CandlePollingResponse(true, false, candle.CloseTime, candle);
            m_currencyDataProviderMock
                .Setup(m => m.GetLastCandle(s_currency, pollingStartTime))
                .Returns(candle);
            
            var candleCryptoPolling = new CandleCryptoPolling(m_currencyDataProviderMock.Object,
                m_systemClock, s_delayTimeInSeconds,
                s_candleSize,
                s_minPrice,
                s_maxPrice);
            
            // Act
            CandlePollingResponse actualCandlePollingResponse = (CandlePollingResponse)await candleCryptoPolling.StartAsync(s_currency, CancellationToken.None, pollingStartTime);

            // Assert
            Assert.AreEqual(expectedCandlePollingResponse, actualCandlePollingResponse);
            m_currencyDataProviderMock.Verify(m=>
                    m.GetLastCandle(It.IsAny<string>(), 
                        It.IsAny<DateTime>()),
                Times.Once);
        }
        
        [TestMethod]
        public async Task When_StartAsync_Given_CandleReachLowerPriceAndHigherPrice_Return_IsBelowTrueAndIsAboveTrue()
        {
            // Arrange
            MyCandle candle = CreateCandle(s_lowerThanMinPrice, s_higherThanMaxPrice);
            DateTime pollingStartTime = candle.CloseTime;
            var expectedCandlePollingResponse = new CandlePollingResponse(true, true, candle.CloseTime, candle);
            m_currencyDataProviderMock
                .Setup(m => m.GetLastCandle(s_currency, pollingStartTime))
                .Returns(candle);
            
            var candleCryptoPolling = new CandleCryptoPolling(m_currencyDataProviderMock.Object,
                m_systemClock, s_delayTimeInSeconds,
                s_candleSize,
                s_minPrice,
                s_maxPrice);
            
            // Act
            CandlePollingResponse actualCandlePollingResponse = (CandlePollingResponse)await candleCryptoPolling.StartAsync(s_currency, CancellationToken.None, pollingStartTime);

            // Assert
            Assert.AreEqual(expectedCandlePollingResponse, actualCandlePollingResponse);
            m_currencyDataProviderMock.Verify(m=>
                    m.GetLastCandle(It.IsAny<string>(), 
                        It.IsAny<DateTime>()),
                Times.Once);
        }
        
        [TestMethod]
        public async Task When_StartAsync_Given_CandleNotReachLowerPriceOrHigherPrice_Should_ContinuePolling()
        {
            // Arrange
            DateTime openTimeFirstCandle = new DateTime(2020,1,1,10,10,0);
            DateTime closeTimeFirstCandle = openTimeFirstCandle.Add(TimeSpan.FromMinutes(s_candleSize));
            DateTime openTimeSecondCandle = closeTimeFirstCandle;
            DateTime closeTimeSecondCandle = openTimeSecondCandle.Add(TimeSpan.FromMinutes(s_candleSize));
            DateTime pollingStartTime = closeTimeFirstCandle;
            var candleNotReachMinOrMaxPrice = new MyCandle(s_maxPrice, s_maxPrice, openTimeFirstCandle, closeTimeFirstCandle, s_higherThanMinPrice, s_lowerThanMaxPrice);
            var candleReachLowerPrice = new MyCandle(s_maxPrice, s_maxPrice, openTimeSecondCandle, closeTimeSecondCandle, s_lowerThanMinPrice, s_lowerThanMaxPrice);
            var expectedCandlePollingResponse = new CandlePollingResponse(true, false, closeTimeSecondCandle, candleReachLowerPrice);
            m_currencyDataProviderMock
                .Setup(m => m.GetLastCandle(s_currency, pollingStartTime))
                .Returns(candleNotReachMinOrMaxPrice);
            m_currencyDataProviderMock
                .Setup(m => m.GetLastCandle(s_currency, pollingStartTime.AddSeconds(s_delayTimeInSeconds)))
                .Returns(candleReachLowerPrice);
            
            var candleCryptoPolling = new CandleCryptoPolling(m_currencyDataProviderMock.Object,
                m_systemClock, s_delayTimeInSeconds,
                s_candleSize,
                s_minPrice,
                s_maxPrice);
            
            // Act
            CandlePollingResponse actualCandlePollingResponse = (CandlePollingResponse)await candleCryptoPolling.StartAsync(s_currency, CancellationToken.None, pollingStartTime);

            // Assert
            Assert.AreEqual(expectedCandlePollingResponse, actualCandlePollingResponse);
            m_currencyDataProviderMock.Verify(m=>
                    m.GetLastCandle(It.IsAny<string>(), 
                        It.IsAny<DateTime>()),
                Times.Exactly(2));
        }
        
        [TestMethod]
        public async Task When_StartAsync_Given_GotCancellationRequest_Should_CandlePollingResponse_IsCancelled_EqualsTrue()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            MyCandle candle = CreateCandle(s_higherThanMinPrice, s_lowerThanMaxPrice);
            DateTime pollingStartTime = candle.CloseTime;
            m_currencyDataProviderMock
                .Setup(m => m.GetLastCandle(s_currency, pollingStartTime))
                .Returns(candle)
                .Callback(()=>cancellationTokenSource.Cancel());
            
            var candleCryptoPolling = new CandleCryptoPolling(m_currencyDataProviderMock.Object,
                m_systemClock, s_delayTimeInSeconds,
                s_candleSize,
                s_minPrice,
                s_maxPrice);
            
            // Act
            CandlePollingResponse actualCandlePollingResponse = (CandlePollingResponse)await candleCryptoPolling.StartAsync(s_currency, cancellationTokenSource.Token, pollingStartTime);

            // Assert
            Assert.IsTrue(actualCandlePollingResponse.IsCancelled);
            Assert.IsNull(actualCandlePollingResponse.Exception);
            m_currencyDataProviderMock.Verify(m=>
                    m.GetLastCandle(It.IsAny<string>(), 
                        It.IsAny<DateTime>()),
                Times.AtLeastOnce);
        }
        
        [TestMethod]
        public async Task When_StartAsync_Given_GotException_Should_CandlePollingResponse_Exception_NotEqualNull()
        {
            // Arrange
            Exception expectedException = new Exception();
            m_currencyDataProviderMock
                .Setup(m => m.GetLastCandle(s_currency, It.IsAny<DateTime>()))
                .Throws(expectedException);
            
            var candleCryptoPolling = new CandleCryptoPolling(m_currencyDataProviderMock.Object,
                m_systemClock, s_delayTimeInSeconds,
                s_candleSize,
                s_minPrice,
                s_maxPrice);
            
            // Act
            CandlePollingResponse actualCandlePollingResponse = (CandlePollingResponse)await candleCryptoPolling.StartAsync(s_currency, CancellationToken.None, DateTime.Now);

            // Assert
            Assert.AreEqual(expectedException, actualCandlePollingResponse.Exception);
            Assert.IsFalse(actualCandlePollingResponse.IsCancelled);
            m_currencyDataProviderMock.Verify(m=>
                    m.GetLastCandle(It.IsAny<string>(), 
                        It.IsAny<DateTime>()),
                Times.Once);
        }

        private static MyCandle CreateCandle(decimal low, decimal high)
        {
            var openTime = new DateTime(2020, 1, 1, 10, 10, 0);
            DateTime closeTime = openTime.Add(TimeSpan.FromMinutes(s_candleSize));
            var candle = new MyCandle(s_maxPrice, s_maxPrice, openTime, closeTime, low, high);
            return candle;
        }
    }
}