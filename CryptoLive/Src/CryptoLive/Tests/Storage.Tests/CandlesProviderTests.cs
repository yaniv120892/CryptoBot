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
        private static readonly DateTime s_currentTime = new DateTime(2020,1,1,10,0,0);
        
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
                s_amountOfCandles,
                s_candleSize,
                s_currentTime);
            
            // Assert
            TimeSpan actualCandlesCoverTime = candles.Span[candles.Length-1].CloseTime - candles.Span[0].OpenTime;
            Assert.AreEqual(expectedCandlesCoverTime, actualCandlesCoverTime);
        }

        [TestMethod]
        public void When_GetLastCandle_Return_CandleWithCorrectOpenAndCloseTime()
        {
            // Arrange
            DateTime expectedCandleOpenTime = s_currentTime.Subtract(TimeSpan.FromMinutes(10));
            DateTime expectedCandleCloseTime = s_currentTime.Subtract(TimeSpan.FromSeconds(1));
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

        private void SetupRepositoryMock()
        {
            var storageCandle1 = CreateStorageCandle(s_currentTime.Subtract(TimeSpan.FromMinutes(s_candleSize * 3)));
            var storageCandle2 = CreateStorageCandle(s_currentTime.Subtract(TimeSpan.FromMinutes(s_candleSize * 2)));
            var storageCandle3 = CreateStorageCandle(s_currentTime.Subtract(TimeSpan.FromMinutes(s_candleSize)));
            m_repositoryMock = new Mock<IRepository<CandleStorageObject>>();
            m_repositoryMock.Setup(m => m.Get(s_currency, s_currentTime.Subtract(TimeSpan.FromMinutes(s_candleSize * 2))))
                .Returns(storageCandle1);
            m_repositoryMock.Setup(m => m.Get(s_currency, s_currentTime.Subtract(TimeSpan.FromMinutes(s_candleSize))))
                .Returns(storageCandle2);
            m_repositoryMock.Setup(m => m.Get(s_currency, s_currentTime))
                .Returns(storageCandle3);
        }

        private static CandleStorageObject CreateStorageCandle(DateTime openTime) =>
            new CandleStorageObject(
                new MyCandle(
                    1,
                    2,
                    openTime, 
                    openTime.AddMinutes(s_candleSize).AddSeconds(-1),
                    1,
                    2));
    }
}