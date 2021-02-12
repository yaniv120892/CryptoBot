using System;
using System.Threading.Tasks;
using Common;

namespace Storage.Abstractions
{
    public interface ICurrencyDataProvider
    {
        Task<decimal> GetPriceAsync(string currency, DateTime currentTime);
        decimal GetRsi(string currency, DateTime currentTime);
        PriceAndRsi GetRsiAndClosePrice(string currency, int candleSizeInMinutes, DateTime currentTime);
        (MyCandle prevCandle , MyCandle currCandle) GetLastCandles(string currency, int candleSizeInMinutes, DateTime currentTime);
        Memory<MyCandle> GetCandles(string currency, int candleSizeInMinutes, int candlesAmount, DateTime currentDateTime);
        decimal GetMacdHistogram(string currency, int candleSizeInMinutes, DateTime currentTime);
    }
}