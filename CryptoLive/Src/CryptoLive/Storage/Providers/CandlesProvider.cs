using System;
using Common;
using Common.DataStorageObjects;
using Storage.Abstractions.Providers;
using Storage.Abstractions.Repository;

namespace Storage.Providers
{
    public class CandlesProvider : ICandlesProvider
    {
        private readonly IRepository<CandleStorageObject> m_repository;

        public CandlesProvider(IRepository<CandleStorageObject> repository)
        {
            m_repository = repository;
        }

        public Memory<MyCandle> GetCandles(string currency, 
            int amountOfCandles, 
            DateTime currentTime)
        {
            Memory<MyCandle> ans = new MyCandle[amountOfCandles];
            DateTime time = AlignTimeToRepositoryKeyFormat(currentTime);
            int counter = amountOfCandles -1;
            while (counter >= 0)
            {
                MyCandle candle = m_repository.Get(currency, time).Candle;
                ans.Span[counter] = candle;
                counter--;
                time = candle.OpenTime.AddSeconds(-1);
            }

            return ans;
        }

        private static DateTime AlignTimeToRepositoryKeyFormat(DateTime currentTime) =>
            currentTime.Second != 59 ?
                currentTime.Subtract(TimeSpan.FromSeconds(currentTime.Second + 1)) :
                currentTime;

        public MyCandle GetLastCandle(string currency, DateTime currentTime)
        {
            Memory<MyCandle> candles = GetCandles(currency, 1, currentTime);
            return candles.Span[0];
        }
    }
}