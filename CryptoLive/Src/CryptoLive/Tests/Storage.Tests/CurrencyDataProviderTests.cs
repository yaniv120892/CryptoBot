using System;
using Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Storage.Abstractions.Providers;
using Storage.Providers;

namespace Storage.Tests
{
    [TestClass]
    public class CurrencyDataProviderTests
    {
        private static readonly string s_currency = "CurrencyName";

        private Mock<ICandlesProvider> m_candlesProviderMock;
        private Mock<IRsiProvider> m_rsiProviderMock;
        private static int s_candleSize = 5;

        [TestMethod]
        public void When_GetRsiAndClosePrice_Given_CurrentTimeIsEndOfMinute_Return_RsiAndPriceOfLastCandleCloseTime()
        {
            // Arrange
            const decimal expectedPrice = 10;
            const decimal expectedRsi = 30;
            DateTime expectedCandleCloseTime = new DateTime(2020,1,1,10,0,59);
            DateTime requestedTimeEndOfMinute = expectedCandleCloseTime;
            
            m_candlesProviderMock = new Mock<ICandlesProvider>();
            m_candlesProviderMock.Setup(m => m.GetLastCandle(s_currency, expectedCandleCloseTime))
                .Returns(CreateMyCandle(expectedCandleCloseTime, expectedPrice));
            
            m_rsiProviderMock = new Mock<IRsiProvider>();
            m_rsiProviderMock.Setup(m => m.Get(s_currency, expectedCandleCloseTime))
                .Returns(expectedRsi);

            var priceProviderMock = new Mock<IPriceProvider>();
            var macdProviderMock = new Mock<IMacdProvider>();
            var sut = new CurrencyDataProvider(priceProviderMock.Object, 
                m_candlesProviderMock.Object, 
                m_rsiProviderMock.Object, 
                macdProviderMock.Object);

            // Act
            PriceAndRsi priceAndRsi = sut.GetRsiAndClosePrice(s_currency, requestedTimeEndOfMinute);

            // Assert
            Assert.AreEqual(expectedPrice, priceAndRsi.Price);
            Assert.AreEqual(expectedRsi, priceAndRsi.Rsi);
        }
        
        [TestMethod]
        public void When_GetRsiAndClosePrice_Given_CurrentTimeIsNotEndOfMinute_Return_RsiAndPriceOfLastCandleCloseTime()
        {
            // Arrange
            const decimal expectedPrice = 10;
            const decimal expectedRsi = 30;
            DateTime expectedCandleCloseTime = new DateTime(2020,1,1,10,0,59);
            DateTime requestedTimeEndOfMinute = new DateTime(2020,1,1,10,1,30);
            
            m_candlesProviderMock = new Mock<ICandlesProvider>();
            m_candlesProviderMock.Setup(m => m.GetLastCandle(s_currency, requestedTimeEndOfMinute))
                .Returns(CreateMyCandle(expectedCandleCloseTime, expectedPrice));
            
            m_rsiProviderMock = new Mock<IRsiProvider>();
            m_rsiProviderMock.Setup(m => m.Get(s_currency, expectedCandleCloseTime))
                .Returns(expectedRsi);

            var priceProviderMock = new Mock<IPriceProvider>();
            var macdProviderMock = new Mock<IMacdProvider>();
            var sut = new CurrencyDataProvider(priceProviderMock.Object, 
                m_candlesProviderMock.Object, 
                m_rsiProviderMock.Object, 
                macdProviderMock.Object);

            // Act
            PriceAndRsi priceAndRsi = sut.GetRsiAndClosePrice(s_currency, requestedTimeEndOfMinute);

            // Assert
            Assert.AreEqual(expectedPrice, priceAndRsi.Price);
            Assert.AreEqual(expectedRsi, priceAndRsi.Rsi);
        }

        private static MyCandle CreateMyCandle(DateTime closeTime, decimal expectedClosePrice) =>
                new MyCandle(
                    1,
                    expectedClosePrice,
                    GetCandleOpenTime(closeTime), 
                    closeTime,
                    1,
                    2);

        private static DateTime GetCandleOpenTime(DateTime closeTime) => 
            closeTime.Subtract(TimeSpan.FromMinutes(s_candleSize-1)).AddSeconds(-closeTime.Second);
    }
}