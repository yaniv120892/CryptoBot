using System;
using System.Threading.Tasks;

namespace CryptoBot.Abstractions
{
    public interface IBuyCryptoTrader
    {
        Task<long> BuyAsync(string currency, decimal price, decimal quantity, DateTime currentTime);
    }
}