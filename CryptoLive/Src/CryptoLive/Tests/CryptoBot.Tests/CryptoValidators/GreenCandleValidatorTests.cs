using System;
using Common;
using CryptoBot.CryptoValidators;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Storage.Abstractions.Providers;

namespace CryptoBot.Tests.CryptoValidators
{ 
    [TestClass]
    public class GreenCandleValidatorTests
    {
        private static readonly string s_currency = "CurrencyName";
        private static readonly int s_candleSize = 15;

        private readonly Mock<ICurrencyDataProvider> m_currencyDataProviderMock = new Mock<ICurrencyDataProvider>();

        [TestMethod]
        public void When_Validate_Given_CurrentCloseHigherThanCurrentOpen_And_CurrentCloseHigherThanPreviousHigh_Return_True()
        {
            // Arrange
            const decimal openValuePrevious = 10;
            const decimal closeValuePrevious = 8;
            const decimal highValuePreviousCandle = 11;
            const decimal openValueCurrent = closeValuePrevious;
            const decimal closeValueCurrent = highValuePreviousCandle + 10;
            var validatorStartTime = new DateTime(2020, 1, 1, 10, 10, 0);

            MyCandle previousCandle = new MyCandle(openValuePrevious,
                closeValuePrevious, 
                validatorStartTime.AddMinutes(-2 * s_candleSize),
                validatorStartTime.AddMinutes(-s_candleSize), 
                closeValuePrevious, 
                highValuePreviousCandle);
            MyCandle currentCandle = new MyCandle(openValueCurrent,
                closeValueCurrent, 
                validatorStartTime.AddMinutes(-s_candleSize),
                validatorStartTime,
                openValueCurrent, 
                closeValueCurrent);     
            var greenCandleValidator = new GreenCandleValidator(m_currencyDataProviderMock.Object);
            m_currencyDataProviderMock
                .Setup(m => m.GetLastCandles(s_currency, s_candleSize,validatorStartTime))
                .Returns((previousCandle, currentCandle));
            bool actual = greenCandleValidator.Validate(s_currency, s_candleSize,validatorStartTime);

            Assert.IsTrue(actual);
        }
        
        [TestMethod]
        public void When_Validate_Given_CurrentCloseHigherThanCurrentOpen_And_CurrentCloseLowerThanPreviousHigh_Return_False()
        {
            // Arrange
            const decimal openValuePrevious = 10;
            const decimal closeValuePrevious = 8;
            const decimal highValuePreviousCandle = 11;
            const decimal openValueCurrent = closeValuePrevious;
            const decimal closeValueCurrent = highValuePreviousCandle - 1;
            var validatorStartTime = new DateTime(2020, 1, 1, 10, 10, 0);

            MyCandle previousCandle = new MyCandle(openValuePrevious,
                closeValuePrevious, 
                validatorStartTime.AddMinutes(-2 * s_candleSize),
                validatorStartTime.AddMinutes(-s_candleSize), 
                closeValuePrevious, 
                highValuePreviousCandle);
            MyCandle currentCandle = new MyCandle(openValueCurrent,
                closeValueCurrent, 
                validatorStartTime.AddMinutes(-s_candleSize),
                validatorStartTime,
                openValueCurrent, 
                closeValueCurrent);     
            var greenCandleValidator = new GreenCandleValidator(m_currencyDataProviderMock.Object);
            m_currencyDataProviderMock
                .Setup(m => m.GetLastCandles(s_currency, s_candleSize,validatorStartTime))
                .Returns((previousCandle, currentCandle));
            bool actual = greenCandleValidator.Validate(s_currency, s_candleSize,validatorStartTime);

            Assert.IsFalse(actual);
        }
        
        [TestMethod]
        public void When_Validate_Given_CurrentCloseLowerThanCurrentOpen_Return_False()
        {
            // Arrange
            const decimal openValuePrevious = 10;
            const decimal closeValuePrevious = 8;
            const decimal highValuePreviousCandle = 11;
            const decimal openValueCurrent = closeValuePrevious;
            const decimal closeValueCurrent = openValueCurrent - 1;
            var validatorStartTime = new DateTime(2020, 1, 1, 10, 10, 0);

            MyCandle previousCandle = new MyCandle(openValuePrevious,
                closeValuePrevious, 
                validatorStartTime.AddMinutes(-2 * s_candleSize),
                validatorStartTime.AddMinutes(-s_candleSize), 
                closeValuePrevious, 
                highValuePreviousCandle);
            MyCandle currentCandle = new MyCandle(openValueCurrent,
                closeValueCurrent, 
                validatorStartTime.AddMinutes(-s_candleSize),
                validatorStartTime,
                openValueCurrent, 
                closeValueCurrent);     
            var greenCandleValidator = new GreenCandleValidator(m_currencyDataProviderMock.Object);
            m_currencyDataProviderMock
                .Setup(m => m.GetLastCandles(s_currency, s_candleSize,validatorStartTime))
                .Returns((previousCandle, currentCandle));
            bool actual = greenCandleValidator.Validate(s_currency, s_candleSize,validatorStartTime);

            Assert.IsFalse(actual);
        }
    }
}