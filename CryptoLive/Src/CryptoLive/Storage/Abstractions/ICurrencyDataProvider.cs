using System;
using System.Threading.Tasks;
using Common;

namespace Storage.Abstractions
{
    public interface ICurrencyDataProvider
    {
        Task<decimal> GetPriceAsync(string desiredSymbol, DateTime currentTime);
        decimal GetRsi(string desiredSymbol, DateTime currentTime);
        PriceAndRsi GetRsiAndClosePrice(string desiredSymbol, int candleSizeInMinutes, DateTime currentTime);
        (MyCandle prevCandle , MyCandle currCandle) GetLastCandles(string desiredSymbol, int candleSizeInMinutes, DateTime currentTime);
        Memory<MyCandle> GetCandles(string desiredSymbol, int candleSizeInMinutes, int candlesAmount, DateTime currentDateTime);
        decimal GetMacdHistogram(string desiredSymbol, int candleSizeInMinutes, DateTime currentTime);
    }
}