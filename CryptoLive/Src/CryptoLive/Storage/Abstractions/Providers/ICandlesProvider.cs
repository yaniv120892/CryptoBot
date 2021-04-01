using System;
using Common;

namespace Storage.Abstractions.Providers
{
    public interface ICandlesProvider
    {
        Memory<MyCandle> GetCandles(string currency, int amountOfCandles, int candleSize, DateTime currentTime);
        MyCandle GetLastCandle(string currency, int candleSize, DateTime currentTime);
    }
}