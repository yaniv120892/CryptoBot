using System;
using System.Threading.Tasks;
using Common;

namespace Utils.Abstractions
{
    public interface ICurrencyService
    {
        Task<decimal> GetPriceAsync(string desiredSymbol, DateTime currentTime);
        Task<decimal> GetRsiAsync(string desiredSymbol, int candleSizeInMinutes, DateTime currentTime, int candlesAmount);
        Task<(MyCandle prevCandle , MyCandle currCandle)> GetLastCandlesAsync(string desiredSymbol, int candleSizeInMinutes, DateTime currentTime);
        Task<MyCandle[]> GetCandlesAsync(string desiredSymbol, int candleSizeInMinutes, int candlesAmount, DateTime currentDateTime);
    }
}