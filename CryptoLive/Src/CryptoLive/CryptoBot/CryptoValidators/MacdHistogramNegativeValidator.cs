using System;
using CryptoBot.Abstractions;
using Infra;
using Microsoft.Extensions.Logging;
using Storage.Abstractions;

namespace CryptoBot.CryptoValidators
{
    public class MacdHistogramNegativeValidator : ICryptoValidator
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<MacdHistogramNegativeValidator>();

        private readonly ICurrencyDataProvider m_currencyDataProvider;
        private readonly INotificationService m_notificationService;
        private readonly int m_candleSizeInMinutes;

        public MacdHistogramNegativeValidator(            
            INotificationService notificationService, 
            ICurrencyDataProvider currencyDataProvider,
            int candleSizeInMinutes)
        {
            m_currencyDataProvider = currencyDataProvider;
            m_candleSizeInMinutes = candleSizeInMinutes;
            m_notificationService = notificationService;
        }

        public bool Validate(string symbol, DateTime time)
        {
            decimal macdHistogram = m_currencyDataProvider.GetMacdHistogram(symbol, m_candleSizeInMinutes, time);
            if (macdHistogram < 0)
            {
                string message = $"{symbol}: MACD histogram is negative, {macdHistogram}, {time}";
                m_notificationService.Notify(message);
                s_logger.LogInformation(message);
                return true;
            }

            s_logger.LogDebug($"{symbol}: MACD histogram is positive, {macdHistogram}, {time}");
            return false;
        }
    }
}