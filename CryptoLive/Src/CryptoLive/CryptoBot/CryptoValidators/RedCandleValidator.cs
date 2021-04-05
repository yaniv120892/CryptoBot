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
        private static readonly string s_actionName = "Red Candle Validator";
        private static readonly double s_candlePercentSize = 0.01;

        private readonly ICurrencyDataProvider m_currencyDataProvider;

        public RedCandleValidator(ICurrencyDataProvider currencyDataProvider)
        {
            m_currencyDataProvider = currencyDataProvider;
        }

        public bool Validate(string currency, int candleSize, DateTime currentTime)
        {
            string message;
            MyCandle currCandle = m_currencyDataProvider.GetLastCandle(currency, candleSize, currentTime);
            if (currCandle.Close < currCandle.Open * (decimal) (1 - s_candlePercentSize))
            {
                message = $"{currency} {s_actionName} done, {currCandle} ,{currentTime:dd/MM/yyyy HH:mm:ss}";
                s_logger.LogInformation(message);
                return true;
            }

            message = $"{currency} {s_actionName} done, Candle is not red {currCandle} ,{currentTime:dd/MM/yyyy HH:mm:ss}";
            s_logger.LogInformation(message);
            return false;
        }
    }
}