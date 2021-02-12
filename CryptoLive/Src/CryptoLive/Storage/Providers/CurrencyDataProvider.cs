using System;
using System.Threading.Tasks;
using Common;
using Storage.Abstractions;

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

        public PriceAndRsi GetRsiAndClosePrice(string currency, int candleSizeInMinutes, DateTime currentTime) =>
            GetRsiAndPriceImpl(currency, candleSizeInMinutes, currentTime);

        public (MyCandle prevCandle, MyCandle currCandle) GetLastCandles(string currency, int candleSizeInMinutes, DateTime currentTime) => 
            GetLastCandlesImpl(currency, candleSizeInMinutes, currentTime);

        public decimal GetMacdHistogram(string currency, int candleSizeInMinutes, DateTime currentTime) => 
            m_macdProvider.Get(currency, currentTime);

        private PriceAndRsi GetRsiAndPriceImpl(string currency, int candleSizeInMinutes, DateTime currentTime)
        {
            MyCandle lastCandle = m_candlesProvider.GetLastCandle(currency, candleSizeInMinutes, currentTime);
            decimal rsi = m_rsiProvider.Get(currency, currentTime);
            return new PriceAndRsi(lastCandle.Close, rsi, lastCandle.CloseTime);
        }
        
        private (MyCandle prevCandle, MyCandle currCandle) GetLastCandlesImpl(string currency, int candleSizeInMinutes,
            DateTime currentTime)
        {
            Memory<MyCandle> lastCandles = m_candlesProvider.GetCandles(currency, candleSizeInMinutes, 2, currentTime);
            MyCandle prevCandle = lastCandles.Span[0];
            MyCandle currCandle = lastCandles.Span[1];
            return (prevCandle, currCandle);
        }
    }
}
