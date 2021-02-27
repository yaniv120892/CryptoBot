using System;
using Common;
using CryptoBot.Abstractions;
using Infra;
using Microsoft.Extensions.Logging;
using Storage.Abstractions.Providers;

namespace CryptoBot.CryptoValidators
{
    public class GreenCandleValidator : ICryptoValidator
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<GreenCandleValidator>();
        private static readonly string s_actionName = "Green Candle Validator";

        private readonly ICurrencyDataProvider m_currencyDataProvider;
        private readonly INotificationService m_notificationService;

        public GreenCandleValidator( 
            INotificationService notificationService, 
            ICurrencyDataProvider currencyDataProvider)
        {
            m_currencyDataProvider = currencyDataProvider;
            m_notificationService = notificationService;
        }
        
        public bool Validate(string currency, DateTime currentTime)
        {
            (MyCandle prevCandle, MyCandle currCandle) = m_currencyDataProvider.GetLastCandles(currency, currentTime);

            string message;
            if (currCandle.Close < currCandle.Open)
            {
                message =
                    $"{currency} {s_actionName} done, Candle is not green {currCandle}, {currentTime}";
                m_notificationService.Notify(message);
                return false;
            }
            s_logger.LogInformation($"{currency}: Candle is green, {currCandle} ,{currentTime}");
            
            // if (currCandle.Close < currCandle.Open * (decimal)1.005)
            // {
            //     s_logger.LogDebug($"{currency}: Candle increase is less than 1%, {currCandle} ,{time}");
            //     return false;
            // }
            // s_logger.LogInformation($"{currency}: Candle increase is above 1%, {currCandle} ,{currentTime}");
            
            if (prevCandle.High < currCandle.Close)
            {
                message = $"{currency} {s_actionName} done, {prevCandle}, {currCandle} ,{currentTime}";
                m_notificationService.Notify(message);
                s_logger.LogInformation(message);
                return true;
            }

            message =
                $"{currency} {s_actionName} done, Previous.High is larger than Current.Close, {prevCandle}, {currCandle} ,{currentTime}";
            m_notificationService.Notify(message);
            s_logger.LogInformation(message);
            return false;
        }
    }
}

    