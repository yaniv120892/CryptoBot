using System;
using Common;
using Common.DataStorageObjects;
using Storage.Abstractions;

namespace Storage.Providers
{
    public class CandlesProvider : ICandlesProvider
    {
        private readonly IRepository<CandleStorageObject> m_repository;

        public CandlesProvider(IRepository<CandleStorageObject> repository)
        {
            m_repository = repository;
        }

        public Memory<MyCandle> GetCandles(string desiredSymbol, 
            int amountOfCandles, 
            int candleSize,
            DateTime currentTime)
        {
            Memory<MyCandle> ans = new MyCandle[amountOfCandles];
            DateTime time = currentTime;
            for (int i = ans.Length-1; i >= 0; i-- , time = time.Subtract(TimeSpan.FromMinutes(candleSize)))
            {
                ans.Span[i] = m_repository.Get(desiredSymbol, time).Candle;
            }

            return ans;
        }
        
        public MyCandle GetLastCandle(string desiredSymbol, int candleSize, DateTime currentTime)
        {
            Memory<MyCandle> candles = GetCandles(desiredSymbol, 1, candleSize, currentTime);
            return candles.Span[0];
        }
    }
}