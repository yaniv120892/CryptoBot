using System;
using System.Threading.Tasks;
using Common;
using Infra;
using Microsoft.Extensions.Logging;
using Utils.Abstractions;

namespace Utils.CryptoValidators
{
    public class RedCandleValidator : ICryptoValidator
    {        
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<RedCandleValidator>();

        private readonly ICurrencyService m_currencyService;
        private readonly INotificationHandler m_notificationHandler;
        private readonly int m_candleSizeInMinutes;

        public RedCandleValidator(INotificationHandler notificationHandler,
            ICurrencyService currencyService, 
            int candleSizeInMinutes)
        {
            m_notificationHandler = notificationHandler;
            m_currencyService = currencyService;
            m_candleSizeInMinutes = candleSizeInMinutes;
        }

        public async Task<bool> Validate(string desiredSymbol, DateTime time )
        {
            s_logger.LogInformation("Start crypto validate red candle");
            (MyCandle _, MyCandle currCandle) = await m_currencyService.GetLastCandlesAsync(desiredSymbol, m_candleSizeInMinutes, time);

            if (IsGreenCandle(currCandle))
            {
                s_logger.LogWarning("Current candle is green");
                return false;
            }

            m_notificationHandler.NotifyIfNeeded(1, desiredSymbol);
            s_logger.LogInformation("Done crypto validate red candle");
            return true;
        }

        private static bool IsGreenCandle(MyCandle currCandle)
        {
            return currCandle.Close >= currCandle.Open;
        }
    }
}