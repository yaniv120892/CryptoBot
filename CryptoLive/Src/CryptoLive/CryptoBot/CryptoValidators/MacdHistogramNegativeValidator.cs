using System;
using CryptoBot.Abstractions;
using Infra;
using Microsoft.Extensions.Logging;
using Storage.Abstractions.Providers;

namespace CryptoBot.CryptoValidators
{
    public class MacdHistogramNegativeValidator : ICryptoValidator
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<MacdHistogramNegativeValidator>();

        private readonly ICurrencyDataProvider m_currencyDataProvider;

        public MacdHistogramNegativeValidator(            
            ICurrencyDataProvider currencyDataProvider)
        {
            m_currencyDataProvider = currencyDataProvider;
        }

        public bool Validate(string currency, DateTime currentTime)
        {
            decimal macdHistogram = m_currencyDataProvider.GetMacdHistogram(currency, currentTime);
            if (macdHistogram < 0)
            {
                string message = $"{currency}: MACD histogram is negative, {macdHistogram}, {currentTime}";
                s_logger.LogInformation(message);
                return true;
            }

            s_logger.LogDebug($"{currency}: MACD histogram is positive, {macdHistogram}, {currentTime}");
            return false;
        }
    }
}