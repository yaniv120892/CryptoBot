using System;
using Common;

namespace Storage.Abstractions.Providers
{
    public interface ICurrencyDataProvider
    {
        decimal GetPriceAsync(string currency, DateTime currentTime);
        decimal GetRsi(string currency, DateTime currentTime);
        decimal GetMeanAverage(string currency, DateTime currentTime);
        PriceAndRsi GetRsiAndClosePrice(string currency, DateTime currentTime);
        (MyCandle prevCandle , MyCandle currCandle) GetLastCandles(string currency, int candleSize, DateTime currentTime);
        MyCandle GetLastCandle(string currency, int candleSize, DateTime currentTime);
    }
}