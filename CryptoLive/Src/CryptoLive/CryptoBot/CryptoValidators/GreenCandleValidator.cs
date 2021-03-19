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

        public GreenCandleValidator(ICurrencyDataProvider currencyDataProvider)
        {
            m_currencyDataProvider = currencyDataProvider;
        }
        
        public bool Validate(string currency, DateTime currentTime)
        {
            (MyCandle prevCandle, MyCandle currCandle) = m_currencyDataProvider.GetLastCandles(currency, currentTime);

            string message;
            if (currCandle.Close < currCandle.Open)
            {
                return false;
            }

            if (prevCandle.High < currCandle.Close)
            {
                message = $"{currency} {s_actionName} done, {prevCandle}, {currCandle} ,{currentTime}";
                s_logger.LogInformation(message);
                return true;
            }

            message =
                $"{currency} {s_actionName} done, Previous.High is larger than Current.Close, {prevCandle}, {currCandle} ,{currentTime}";
            s_logger.LogInformation(message);
            return false;
        }
    }
}

    