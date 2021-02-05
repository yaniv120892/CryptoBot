using System;
using System.Threading.Tasks;

namespace Storage.Abstractions
{
    public interface IPriceProvider
    {
        Task<decimal> GetPrice(string symbol, DateTime currentTime);
    }
}