using System;
using System.Threading.Tasks;

namespace CryptoBot.Abstractions
{
    public interface IBuyCryptoTrader
    {
        Task<(decimal buyPrice, decimal quantity)> Buy(string currency, decimal quoteOrderQuantity, DateTime currentTime);
    }
}