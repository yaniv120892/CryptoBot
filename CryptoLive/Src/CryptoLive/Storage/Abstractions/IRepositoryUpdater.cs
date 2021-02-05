using System;
using Common;

namespace Storage.Abstractions
{
    public interface IRepositoryUpdater
    {
        void AddInfo(MyCandle candle, DateTime candleTime, DateTime newCandleTime);
    }
}