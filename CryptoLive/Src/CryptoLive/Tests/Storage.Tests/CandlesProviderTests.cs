using System;
using Common;
using Common.DataStorageObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Storage.Abstractions.Repository;
using Storage.Providers;

namespace Storage.Tests
{
    [TestClass]
    public class CandlesProviderTests
    {
        private Mock<IRepository<CandleStorageObject>> m_repositoryMock;
        private static readonly string s_currency = "CurrencyName";
        private static readonly int s_amountOfCandles = 3;
        private static readonly int s_candleSize = 10;
        private static readonly DateTime s_currentTime = new DateTime(2020,1,1,10,0,59);
        
        [TestMethod]
        public void When_GetLastCandles_Return_CorrectAmountOfCandles()
        {
            // Arrange
            int expectedAmountOfCandles = s_amountOfCandles;
            SetupRepositoryMock();
            var sut = new CandlesProvider(m_repositoryMock.Object);

            // Act
            Memory<MyCandle> candles = sut.GetCandles(s_currency,
                s_amountOfCandles,
                s_candleSize,
                s_currentTime);
            int actualAmountOfCandles = candles.Length;
            
            // Assert
            Assert.AreEqual(expectedAmountOfCandles, actualAmountOfCandles);
        }

        [TestMethod]
        public void When_GetLastCandles_Return_CandlesWithCorrectSize()
        {
            // Arrange
            TimeSpan expectedCandleSize = new TimeSpan(0,9,59);
            SetupRepositoryMock();
            var sut = new CandlesProvider(m_repositoryMock.Object);

            // Act
            Memory<MyCandle> candles = sut.GetCandles(s_currency,
                s_amountOfCandles,
                s_candleSize,
                s_currentTime);
            
            // Assert
            foreach (MyCandle candle in candles.Span)
            {
                TimeSpan actualCandleSize = candle.CloseTime - candle.OpenTime;
                Assert.AreEqual(expectedCandleSize, actualCandleSize);
            }
        }
        
        [TestMethod]
        public void When_GetLastCandles_Return_CandlesThatCoverRequiredTime()
        {
            // Arrange
            TimeSpan expectedCandlesCoverTime = new TimeSpan(0,29,59);
            SetupRepositoryMock();
            var sut = new CandlesProvider(m_repositoryMock.Object);

            // Act
            Memory<MyCandle> candles = sut.GetCandles(s_currency,
                s_candleSize,
                s_amountOfCandles,
                s_currentTime);
            
            // Assert
            TimeSpan actualCandlesCoverTime = candles.Span[candles.Length-1].CloseTime - candles.Span[0].OpenTime;
            Assert.AreEqual(expectedCandlesCoverTime, actualCandlesCoverTime);
        }

        [TestMethod]
        public void When_GetLastCandle_Given_CurrentTimeIsEndOfMinute_Return_CandleWithCorrectOpenAndCloseTime()
        {
            // Arrange
            DateTime expectedCandleOpenTime = new DateTime(2020,1,1,9,51,0);
            DateTime expectedCandleCloseTime = new DateTime(2020,1,1,10,0,59);
            SetupRepositoryMock();
            var sut = new CandlesProvider(m_repositoryMock.Object);

            // Act
            MyCandle candle = sut.GetLastCandle(s_currency,
                s_candleSize,
                s_currentTime);
            
            // Assert
            Assert.AreEqual(expectedCandleCloseTime, candle.CloseTime);
            Assert.AreEqual(expectedCandleOpenTime, candle.OpenTime);
        }
        
        [TestMethod]
        public void When_GetLastCandle_Given_CurrentTimeIsMiddleOfMinute_Return_CandleWithCorrectOpenAndCloseTime()
        {
            // Arrange
            DateTime expectedCandleOpenTime = new DateTime(2020,1,1,9,50,0);
            DateTime expectedCandleCloseTime = new DateTime(2020,1,1,9,59,59);
            DateTime currentTime = new DateTime(2020,1,1,10,0,40);
            SetupRepositoryMock();
            var sut = new CandlesProvider(m_repositoryMock.Object);

            // Act
            MyCandle candle = sut.GetLastCandle(s_currency, s_candleSize, currentTime);
            
            // Assert
            Assert.AreEqual(expectedCandleCloseTime, candle.CloseTime);
            Assert.AreEqual(expectedCandleOpenTime, candle.OpenTime);
        }

        private void SetupRepositoryMock()
        {
            m_repositoryMock = new Mock<IRepository<CandleStorageObject>>();
            for (int i = 0; i < s_candleSize * s_amountOfCandles; i++)
            {
                var storageCandle = CreateStorageCandle(s_currentTime.Subtract(TimeSpan.FromMinutes(i)));
                m_repositoryMock.Setup(m => m.Get(s_currency, storageCandle.Candle.CloseTime))
                    .Returns(storageCandle);
            }
        }

        private static CandleStorageObject CreateStorageCandle(DateTime closeTime) =>
            new CandleStorageObject(
                new MyCandle(
                    1,
                    2,
                    GetCandleOpenTime(closeTime), 
                    closeTime,
                    1,
                    2));

        private static DateTime GetCandleOpenTime(DateTime closeTime) => 
            closeTime.AddSeconds(-closeTime.Second);
    }
}