using System;
using Common;
using CryptoBot.Abstractions;
using Infra;
using Microsoft.Extensions.Logging;
using Storage.Abstractions;

namespace CryptoBot.CryptoValidators
{
    public class GreenCandleValidator : ICryptoValidator
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<GreenCandleValidator>();

        private readonly ICurrencyDataProvider m_currencyDataProvider;
        private readonly INotificationService m_notificationService;
        private readonly int m_candleSizeInMinutes;

        public GreenCandleValidator( 
            INotificationService notificationService, 
            ICurrencyDataProvider currencyDataProvider,
            int candleSizeInMinutes)
        {
            m_currencyDataProvider = currencyDataProvider;
            m_candleSizeInMinutes = candleSizeInMinutes;
            m_notificationService = notificationService;
        }
        
        public bool Validate(string symbol, DateTime time )
        {
            (MyCandle prevCandle, MyCandle currCandle) = m_currencyDataProvider.GetLastCandles(symbol, m_candleSizeInMinutes, time);
            
            if (currCandle.Close < currCandle.Open)
            {
                s_logger.LogDebug($"{symbol}: Candle is red, {currCandle} ,{time}");
                return false;
            }
            s_logger.LogDebug($"{symbol}: Candle is green, {currCandle} ,{time}");
            
            if (currCandle.Close < currCandle.Open * (decimal)1.005)
            {
                s_logger.LogDebug($"{symbol}: Candle increase is less than 1%, {currCandle} ,{time}");
                return false;
            }
            
            s_logger.LogDebug($"{symbol}: Candle increase is above 1%, {currCandle} ,{time}");
            if (prevCandle.High < currCandle.Close)
            {
                string message =
                    $"{symbol}: Previous.High is smaller than Current.Close, {prevCandle}, {currCandle} ,{time}";
                m_notificationService.Notify(message);
                s_logger.LogInformation(message);
                return true;
            }

            s_logger.LogDebug($"{symbol}: Previous.High is larger than Current.Close, {prevCandle}, {currCandle} ,{time}");
            return false;
        }
    }
}

    