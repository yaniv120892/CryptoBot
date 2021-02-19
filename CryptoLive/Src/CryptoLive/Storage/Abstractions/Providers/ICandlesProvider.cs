using System;
using Common;

namespace Storage.Abstractions.Providers
{
    public interface ICandlesProvider
    {
        Memory<MyCandle> GetCandles(string currency,
            int amountOfCandles,
            DateTime currentTime);

        MyCandle GetLastCandle(string currency, DateTime currentTime);
    }
}