using System;
using Common;
using CryptoBot.CryptoValidators;
using Infra;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Storage.Abstractions.Providers;

namespace CryptoBot.Tests.CryptoValidators
{
    [TestClass]
    public class RedCandleValidatorTests
    {
        private static readonly string s_currency = "CurrencyName";
        private static readonly int s_candleSize = 15;

        private readonly Mock<INotificationService> m_notificationServiceMock = new Mock<INotificationService>();
        private readonly Mock<ICurrencyDataProvider> m_currencyDataProviderMock = new Mock<ICurrencyDataProvider>();

        [TestMethod]
        public void When_Validate_Given_CloseHigherThanOpen_Return_False()
        {
            // Arrange
            const decimal openValue = 10;
            const decimal closeValue = 15;
            var validatorStartTime = new DateTime(2020, 1, 1, 10, 10, 0);
            MyCandle redCandle = CreateCandle(validatorStartTime, openValue, closeValue);
            var redCandleValidator = new RedCandleValidator(m_currencyDataProviderMock.Object);
            m_currencyDataProviderMock
                .Setup(m => m.GetLastCandle(s_currency, validatorStartTime))
                .Returns(redCandle);
            bool actual = redCandleValidator.Validate(s_currency, validatorStartTime);

            Assert.IsFalse(actual);
        }
        
        [TestMethod]
        public void When_Validate_Given_CloseLowerThanOpen_Return_True()
        {
            // Arrange
            const decimal openValue = 10;
            const decimal closeValue = 5;
            var validatorStartTime = new DateTime(2020, 1, 1, 10, 10, 0);
            MyCandle redCandle = CreateCandle(validatorStartTime, openValue, closeValue);
            var redCandleValidator = new RedCandleValidator(m_currencyDataProviderMock.Object);
            m_currencyDataProviderMock
                .Setup(m => m.GetLastCandle(s_currency, validatorStartTime))
                .Returns(redCandle);
            bool actual = redCandleValidator.Validate(s_currency, validatorStartTime);

            Assert.IsTrue(actual);
        }


        private MyCandle CreateCandle(DateTime candleCloseTime, decimal openValue, decimal closeValue)
        {
            DateTime candleOpenTime = candleCloseTime.AddMinutes(-s_candleSize);
            decimal low = Math.Min(openValue, closeValue) - 1;
            decimal high = Math.Max(openValue, closeValue) + 1;
            return new MyCandle(openValue, closeValue, candleOpenTime, candleCloseTime, low, high);
        }
    }
}