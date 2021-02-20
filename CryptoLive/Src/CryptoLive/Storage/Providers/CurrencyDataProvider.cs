using System;
using System.Threading.Tasks;
using Common;
using Storage.Abstractions.Providers;

namespace Storage.Providers
{
    public class CurrencyDataProvider : ICurrencyDataProvider
    {
        private readonly IPriceProvider m_priceProvider;
        private readonly ICandlesProvider m_candlesProvider;
        private readonly IRsiProvider m_rsiProvider;
        private readonly IMacdProvider m_macdProvider;

        public CurrencyDataProvider(
            IPriceProvider priceProvider,
            ICandlesProvider candlesProvider,
            IRsiProvider rsiProvider,
            IMacdProvider macdProvider)
        {
            m_priceProvider = priceProvider;
            m_candlesProvider = candlesProvider;
            m_rsiProvider = rsiProvider;
            m_macdProvider = macdProvider;
        }

        public async Task<decimal> GetPriceAsync(string currency, DateTime currentTime) => 
            await m_priceProvider.GetPrice(currency, currentTime);

        public decimal GetRsi(string currency, DateTime currentTime) => 
            m_rsiProvider.Get(currency, currentTime);

        public PriceAndRsi GetRsiAndClosePrice(string currency, DateTime currentTime) =>
            GetRsiAndPriceImpl(currency, currentTime);

        public (MyCandle prevCandle, MyCandle currCandle) GetLastCandles(string currency, DateTime currentTime) => 
            GetLastCandlesImpl(currency, currentTime);

        public MyCandle GetLastCandle(string currency, DateTime currentTime)
        {
            (MyCandle _, MyCandle currCandle) = GetLastCandles(currency, currentTime);
            return currCandle;
        }

        public decimal GetMacdHistogram(string currency, DateTime currentTime) => 
            m_macdProvider.Get(currency, currentTime);

        private PriceAndRsi GetRsiAndPriceImpl(string currency, DateTime currentTime)
        {
            MyCandle lastCandle = m_candlesProvider.GetLastCandle(currency, currentTime);
            decimal rsi = m_rsiProvider.Get(currency, lastCandle.CloseTime);
            return new PriceAndRsi(lastCandle.Close, rsi, lastCandle.CloseTime);
        }
        
        private (MyCandle prevCandle, MyCandle currCandle) GetLastCandlesImpl(string currency,
            DateTime currentTime)
        {
            Memory<MyCandle> lastCandles = m_candlesProvider.GetCandles(currency, 2, currentTime);
            MyCandle prevCandle = lastCandles.Span[0];
            MyCandle currCandle = lastCandles.Span[1];
            return (prevCandle, currCandle);
        }
    }
}
