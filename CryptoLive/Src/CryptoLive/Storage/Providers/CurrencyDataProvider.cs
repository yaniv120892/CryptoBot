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

        public async Task<decimal> GetPriceAsync(string symbol, DateTime currentTime)
        {
            decimal price = await m_priceProvider.GetPrice(symbol, currentTime);
            return price;
        }
        
        public decimal GetRsi(string symbol, DateTime currentTime)
        {
            decimal rsi = m_rsiProvider.Get(symbol, currentTime);
            return rsi;
        }

        public PriceAndRsi GetRsiAndClosePrice(string symbol, int candleSizeInMinutes, DateTime currentTime)
        {
                PriceAndRsi priceAndRsi = GetRsiAndPriceImpl(symbol, candleSizeInMinutes , currentTime);
                return priceAndRsi;
        }

        public (MyCandle prevCandle, MyCandle currCandle) GetLastCandles(string symbol, int candleSizeInMinutes, DateTime currentTime)
        {
            Memory<MyCandle> lastCandles = GetCandles(symbol, candleSizeInMinutes, 2, currentTime);
            
            var prevCandle = lastCandles.Span[0];
            var currCandle = lastCandles.Span[1];

            return (prevCandle,currCandle);
        }
        
        public decimal GetMacdHistogram(string symbol, int candleSizeInMinutes, DateTime currentTime)
        {
            decimal macd = m_macdProvider.Get(symbol, currentTime);
            return macd;
        }
        
        public Memory<MyCandle> GetCandles(string symbol, int candleSizeInMinutes, int candlesAmount, DateTime currentTime)
        {
            return m_candlesProvider.GetCandles(symbol, candlesAmount , candleSizeInMinutes, currentTime);
        }

        private PriceAndRsi GetRsiAndPriceImpl(string symbol, int candleSizeInMinutes, DateTime currentTime)
        {
            MyCandle lastCandle = m_candlesProvider.GetLastCandle(symbol, candleSizeInMinutes, currentTime);
            decimal rsi = m_rsiProvider.Get(symbol, currentTime);
            return new PriceAndRsi(lastCandle.Close, rsi, lastCandle.CloseTime);
        }
    }
}
