using System;
using Common;
using Storage.Abstractions.Providers;

namespace Storage.Providers
{
    public class CurrencyDataProvider : ICurrencyDataProvider
    {
        private readonly ICandlesProvider m_candlesProvider;
        private readonly IRsiProvider m_rsiProvider;
        private readonly IMeanAverageProvider m_meanAverageProvider;

        public CurrencyDataProvider(ICandlesProvider candlesProvider,
            IRsiProvider rsiProvider, 
            IMeanAverageProvider meanAverageProvider)
        {
            m_candlesProvider = candlesProvider;
            m_rsiProvider = rsiProvider;
            m_meanAverageProvider = meanAverageProvider;
        }

        public decimal GetPriceAsync(string currency, DateTime currentTime) => 
            GetLastCandle(currency, 1, currentTime).Close;

        public decimal GetRsi(string currency, DateTime currentTime) => 
            m_rsiProvider.Get(currency, currentTime);

        public decimal GetMeanAverage(string currency, DateTime currentTime)
        {
            DateTime time = GetLastCandle(currency, 1, currentTime).CloseTime;
            return m_meanAverageProvider.Get(currency, time);
        }

        public PriceAndRsi GetRsiAndClosePrice(string currency, DateTime currentTime) =>
            GetRsiAndPriceImpl(currency, currentTime);

        public (MyCandle prevCandle, MyCandle currCandle) GetLastCandles(string currency, int candleSize, DateTime currentTime) => 
            GetLastCandlesImpl(currency, candleSize, currentTime);

        public MyCandle GetLastCandle(string currency, int candleSize, DateTime currentTime)
        {
            (MyCandle _, MyCandle currCandle) = GetLastCandles(currency, candleSize, currentTime);
            return currCandle;
        }

        private PriceAndRsi GetRsiAndPriceImpl(string currency, DateTime currentTime)
        {
            MyCandle lastCandle = m_candlesProvider.GetLastCandle(currency, 1, currentTime);
            decimal rsi = m_rsiProvider.Get(currency, lastCandle.CloseTime);
            return new PriceAndRsi(lastCandle.Close, rsi, lastCandle.CloseTime);
        }
        
        private (MyCandle prevCandle, MyCandle currCandle) GetLastCandlesImpl(string currency, int candleSize,
            DateTime currentTime)
        {
            Memory<MyCandle> lastCandles = m_candlesProvider.GetCandles(currency, 2, candleSize, currentTime);
            MyCandle prevCandle = lastCandles.Span[0];
            MyCandle currCandle = lastCandles.Span[1];
            return (prevCandle, currCandle);
        }
    }
}
