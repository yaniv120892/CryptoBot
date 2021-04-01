using System;
using Common;
using Common.DataStorageObjects;
using Storage.Abstractions.Providers;
using Storage.Abstractions.Repository;
using Storage.Repository;
using Utils.Converters;

namespace Storage.Providers
{
    public class CandlesProvider : ICandlesProvider
    {
        private readonly IRepository<CandleStorageObject> m_repository;

        public CandlesProvider(IRepository<CandleStorageObject> repository)
        {
            m_repository = repository;
        }

        public Memory<MyCandle> GetCandles(string currency, int amountOfCandles, int candleSize, DateTime currentTime)
        {
            Memory<MyCandle> ans = new MyCandle[amountOfCandles*candleSize];
            DateTime time = RepositoryKeyConverter.AlignTimeToRepositoryKeyFormat(currentTime);
            int counter = ans.Length -1;
            while (counter >= 0)
            {
                MyCandle candle = m_repository.Get(currency, time).Candle;
                ans.Span[counter] = candle;
                counter--;
                time = candle.OpenTime.AddSeconds(-1);
            }

            return CandleConverter.ConvertByCandleSize(ans.Span, candleSize, amountOfCandles);
        }

        public MyCandle GetLastCandle(string currency, int candleSize, DateTime currentTime)
        {
            Memory<MyCandle> candles = GetCandles(currency, 1, candleSize, currentTime);
            return candles.Span[0];
        }
    }
}