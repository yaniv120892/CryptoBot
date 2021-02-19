using System;
using System.Threading.Tasks;
using Common;

namespace Storage.Abstractions.Providers
{
    public interface ICurrencyDataProvider
    {
        Task<decimal> GetPriceAsync(string currency, DateTime currentTime);
        decimal GetRsi(string currency, DateTime currentTime);
        PriceAndRsi GetRsiAndClosePrice(string currency, DateTime currentTime);
        (MyCandle prevCandle , MyCandle currCandle) GetLastCandles(string currency, DateTime currentTime);
        MyCandle GetLastCandle(string currency, DateTime currentTime);
        decimal GetMacdHistogram(string currency, DateTime currentTime);
    }
}