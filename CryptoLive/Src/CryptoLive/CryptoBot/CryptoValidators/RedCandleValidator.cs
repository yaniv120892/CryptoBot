using System;
using Common;
using CryptoBot.Abstractions;
using Infra;
using Microsoft.Extensions.Logging;
using Storage.Abstractions.Providers;

namespace CryptoBot.CryptoValidators
{
    public class RedCandleValidator : ICryptoValidator
    {        
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<RedCandleValidator>();

        private readonly ICurrencyDataProvider m_currencyDataProvider;
        private readonly INotificationService m_notificationService;

        public RedCandleValidator(            
            INotificationService notificationService,
            ICurrencyDataProvider currencyDataProvider)
        {
            m_currencyDataProvider = currencyDataProvider;
            m_notificationService = notificationService;
        }

        public bool Validate(string currency, DateTime time)
        {
            MyCandle currCandle = m_currencyDataProvider.GetLastCandle(currency, time);
            if (currCandle.Close < currCandle.Open)
            {
                string message = $"{currency}: Candle is red, {currCandle} ,{time}";
                m_notificationService.Notify(message);
                s_logger.LogInformation(message);
                return true;
            }

            s_logger.LogDebug($"{currency}: Candle is green, {currCandle} ,{time}");
            return false;
        }
    }
}