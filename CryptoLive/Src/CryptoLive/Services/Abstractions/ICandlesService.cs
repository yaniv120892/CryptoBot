using System;
using System.Threading.Tasks;
using Common;

namespace Services.Abstractions
{
    public interface ICandlesService
    {
        Task<Memory<MyCandle>> GetOneMinuteCandles(string currency, int candlesAmount, DateTime currentTime);
    }
}