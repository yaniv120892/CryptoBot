using System;
using System.Threading.Tasks;

namespace Storage.Abstractions.Providers
{
    public interface IPriceProvider
    {
        Task<decimal> GetPrice(string currency, DateTime currentTime);
    }
}