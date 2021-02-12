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

        public async Task<decimal> GetPriceAsync(string currency, DateTime currentTime)
        {
            decimal price = await m_priceProvider.GetPrice(currency, currentTime);
            return price;
        }
        
        public decimal GetRsi(string currency, DateTime currentTime)
        {
            decimal rsi = m_rsiProvider.Get(currency, currentTime);
            return rsi;
        }

        public PriceAndRsi GetRsiAndClosePrice(string currency, int candleSizeInMinutes, DateTime currentTime)
        {
                PriceAndRsi priceAndRsi = GetRsiAndPriceImpl(currency, candleSizeInMinutes , currentTime);
                return priceAndRsi;
        }

        public (MyCandle prevCandle, MyCandle currCandle) GetLastCandles(string currency, int candleSizeInMinutes, DateTime currentTime)
        {
            Memory<MyCandle> lastCandles = GetCandles(currency, candleSizeInMinutes, 2, currentTime);
            
            var prevCandle = lastCandles.Span[0];
            var currCandle = lastCandles.Span[1];

            return (prevCandle,currCandle);
        }
        
        public decimal GetMacdHistogram(string currency, int candleSizeInMinutes, DateTime currentTime)
        {
            decimal macd = m_macdProvider.Get(currency, currentTime);
            return macd;
        }
        
        public Memory<MyCandle> GetCandles(string currency, int candleSizeInMinutes, int candlesAmount, DateTime currentTime)
        {
            return m_candlesProvider.GetCandles(currency, candlesAmount , candleSizeInMinutes, currentTime);
        }

        private PriceAndRsi GetRsiAndPriceImpl(string currency, int candleSizeInMinutes, DateTime currentTime)
        {
            MyCandle lastCandle = m_candlesProvider.GetLastCandle(currency, candleSizeInMinutes, currentTime);
            decimal rsi = m_rsiProvider.Get(currency, currentTime);
            return new PriceAndRsi(lastCandle.Close, rsi, lastCandle.CloseTime);
        }
    }
}
