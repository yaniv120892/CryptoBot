using System;
using System.Threading.Tasks;

namespace Services.Abstractions
{
    public interface ITradeService
    {
        Task PlaceSellOcoOrderAsync(string currency, decimal quantity, decimal price, decimal stopAndLimitPrice);
        Task<long> PlaceBuyLimitOrderAsync(string currency, decimal limitPrice, decimal quantity, DateTime currentTime);
        Task CancelOrderAsync(string currency, long orderId);
        Task<string> GetOrderStatusAsync(string currency, long orderId, DateTime currentTime);
    }
}