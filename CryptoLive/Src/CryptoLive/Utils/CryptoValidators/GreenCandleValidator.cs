using System;
using System.Threading.Tasks;
using Common;
using Infra;
using Microsoft.Extensions.Logging;
using Utils.Abstractions;

namespace Utils.CryptoValidators
{
    public class GreenCandleValidator : ICryptoValidator
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<GreenCandleValidator>();

        private readonly ICurrencyService m_currencyService;
        private readonly INotificationHandler m_notificationHandler;
        private readonly int m_candleSizeInMinutes;

        public GreenCandleValidator(INotificationHandler notificationHandler,
            ICurrencyService currencyService,
            int candleSizeInMinutes)
        {
            m_notificationHandler = notificationHandler;
            m_currencyService = currencyService;
            m_candleSizeInMinutes = candleSizeInMinutes;
        }

        public async Task<bool> Validate(string desiredSymbol, DateTime time)
        {
            s_logger.LogInformation("Start crypto validate green candle");
            (MyCandle prevCandle, MyCandle currCandle) =
                await m_currencyService.GetLastCandlesAsync(desiredSymbol, m_candleSizeInMinutes, time);

            if (IsRedCandle(currCandle))
            {
                s_logger.LogWarning("Current candle is red");
                return false;
            }

            if (IsPrevHighCandleHigherThanCurrentCloseCandle(prevCandle, currCandle))
            {
                s_logger.LogWarning("Current close candle is below previous candle high");
                return false;
            }

            m_notificationHandler.NotifyIfNeeded(1, desiredSymbol);
            s_logger.LogInformation("Done crypto validate green candle");
            return true;
        }

        private static bool IsPrevHighCandleHigherThanCurrentCloseCandle(MyCandle prevCandle,
            MyCandle currCandle)
        {
            return prevCandle.High >= currCandle.Close;
        }

        private static bool IsRedCandle(MyCandle currCandle)
        {
            return currCandle.Close <= currCandle.Open;
        }
    }
}

    