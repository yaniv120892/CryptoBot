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
        private readonly INotificationService m_notificationService;

        public MacdHistogramNegativeValidator(            
            INotificationService notificationService, 
            ICurrencyDataProvider currencyDataProvider)
        {
            m_currencyDataProvider = currencyDataProvider;
            m_notificationService = notificationService;
        }

        public bool Validate(string currency, DateTime time)
        {
            decimal macdHistogram = m_currencyDataProvider.GetMacdHistogram(currency, time);
            if (macdHistogram < 0)
            {
                string message = $"{currency}: MACD histogram is negative, {macdHistogram}, {time}";
                m_notificationService.Notify(message);
                s_logger.LogInformation(message);
                return true;
            }

            s_logger.LogDebug($"{currency}: MACD histogram is positive, {macdHistogram}, {time}");
            return false;
        }
    }
}