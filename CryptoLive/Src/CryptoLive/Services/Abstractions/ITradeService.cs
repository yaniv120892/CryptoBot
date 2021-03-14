using System;
using System.Threading.Tasks;

namespace Services.Abstractions
{
    public interface ITradeService
    {
        Task<(decimal buyPrice, decimal quantity)> PlaceBuyMarketOrderAsync(string currency, decimal quoteOrderQuantity,
            DateTime currentTime);
        Task PlaceSellOcoOrderAsync(string currency, decimal quantity, decimal price, decimal stopAndLimitPrice);
    }
}