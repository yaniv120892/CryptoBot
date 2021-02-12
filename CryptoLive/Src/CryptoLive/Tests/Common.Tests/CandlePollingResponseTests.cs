using System;
using Common.PollingResponses;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Common.Tests
{
    [TestClass]
    public class CandlePollingResponseTests
    {
        [TestMethod]
        public void When_IsWin_Given_IsAboveTrue_Return_True()
        {
            // Arrange
            CandlePollingResponse candlePollingResponse =
                new CandlePollingResponse(false, true, DateTime.Now, new MyCandle());

            // Act + Assert
            Assert.IsTrue(candlePollingResponse.IsWin);
        }
        
        [TestMethod]
        public void When_IsWin_Given_IsAboveFalse_Return_False()
        {
            // Arrange
            CandlePollingResponse candlePollingResponse =
                new CandlePollingResponse(false, false, DateTime.Now, new MyCandle());

            // Act + Assert
            Assert.IsFalse(candlePollingResponse.IsWin);
        }
        
        [TestMethod]
        public void When_IsWin_Given_IsAboveTrueAndIsBelowTrue_Return_True()
        {
            // Arrange
            CandlePollingResponse candlePollingResponse =
                new CandlePollingResponse(true, true, DateTime.Now, new MyCandle());

            // Act + Assert
            Assert.IsTrue(candlePollingResponse.IsWin);
        }
    }
}