using System;
using System.Threading.Tasks;

namespace Services.Abstractions
{
    public interface IPriceService
    {
        Task<decimal> GetPrice(string currency, DateTime currentTime);
    }
}