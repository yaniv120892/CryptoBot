using System;
using Common;

namespace Storage.Abstractions
{
    public interface ICandlesProvider
    {
        Memory<MyCandle> GetCandles(string desiredSymbol,
            int amountOfCandles,
            int candleSize,
            DateTime currentTime);

        MyCandle GetLastCandle(string desiredSymbol, int candleSize, DateTime currentTime);
    }
}