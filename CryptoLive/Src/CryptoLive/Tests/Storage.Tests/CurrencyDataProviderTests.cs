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
        private Mock<IMeanAverageProvider> m_meanAverageProviderMock;

        [TestMethod]
        public void When_GetRsiAndClosePrice_Given_CurrentTimeIsEndOfMinute_Return_RsiAndPriceOfLastCandleCloseTime()
        {
            // Arrange
            const decimal expectedPrice = 10;
            const decimal expectedRsi = 30;
            DateTime expectedCandleCloseTime = new DateTime(2020,1,1,10,0,59);
            DateTime requestedTimeEndOfMinute = expectedCandleCloseTime;

            m_candlesProviderMock = new Mock<ICandlesProvider>();
            m_candlesProviderMock
                .Setup(m => m.GetLastCandle(s_currency, 1, expectedCandleCloseTime))
                .Returns(CreateMyCandle(expectedCandleCloseTime, expectedPrice));
            
            m_rsiProviderMock = new Mock<IRsiProvider>();
            m_rsiProviderMock
                .Setup(m => m.Get(s_currency, expectedCandleCloseTime))
                .Returns(expectedRsi);

            m_meanAverageProviderMock = new Mock<IMeanAverageProvider>();
            
            var sut = new CurrencyDataProvider(m_candlesProviderMock.Object, 
                m_rsiProviderMock.Object, m_meanAverageProviderMock.Object);

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
            m_candlesProviderMock
                .Setup(m => m.GetLastCandle(s_currency, 1, requestedTimeEndOfMinute))
                .Returns(CreateMyCandle(expectedCandleCloseTime, expectedPrice));
            
            m_rsiProviderMock = new Mock<IRsiProvider>();
            m_rsiProviderMock.Setup(m => m.Get(s_currency, expectedCandleCloseTime))
                .Returns(expectedRsi);
            
            m_meanAverageProviderMock = new Mock<IMeanAverageProvider>();
            
            var sut = new CurrencyDataProvider(m_candlesProviderMock.Object, 
                m_rsiProviderMock.Object, m_meanAverageProviderMock.Object);
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
            closeTime.AddSeconds(-closeTime.Second);
    }
}