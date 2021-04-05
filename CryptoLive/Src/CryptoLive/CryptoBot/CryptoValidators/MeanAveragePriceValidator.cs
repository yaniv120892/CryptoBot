using System;
using Storage.Abstractions.Providers;

namespace CryptoBot.CryptoValidators
{
    public class MeanAveragePriceValidator
    {
        private readonly ICurrencyDataProvider m_currencyDataProvider;

        public MeanAveragePriceValidator(ICurrencyDataProvider currencyDataProvider)
        {
            m_currencyDataProvider = currencyDataProvider;
        }
        
        public bool ValidateAbove(string currency, DateTime currentTime)
        {
            decimal currentPrice = m_currencyDataProvider.GetPriceAsync(currency, currentTime);
            decimal meanAverage = m_currencyDataProvider.GetMeanAverage(currency, currentTime);
            return currentPrice > meanAverage;
        }
        
        public bool ValidateBelow(string currency, DateTime currentTime)
        {
            decimal currentPrice = m_currencyDataProvider.GetPriceAsync(currency, currentTime);
            decimal meanAverage = m_currencyDataProvider.GetMeanAverage(currency, currentTime);
            return currentPrice < meanAverage;
        }
    }
}