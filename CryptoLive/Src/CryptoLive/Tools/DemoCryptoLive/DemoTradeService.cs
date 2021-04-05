using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Services.Abstractions;
using Storage.Abstractions.Providers;

namespace DemoCryptoLive
{
    internal class DemoTradeService : ITradeService
    {
        private long m_currentOrderId;
        private readonly ICandlesProvider m_candlesProvider;
        private readonly List<OrderIdsAndPrice> m_openOrders;

        public DemoTradeService(ICandlesProvider candlesProvider)
        {
            m_candlesProvider = candlesProvider;
            m_openOrders = new List<OrderIdsAndPrice>();
        }

        public Task PlaceSellOcoOrderAsync(string currency, decimal quantity, decimal price, decimal stopAndLimitPrice)
        {
            return Task.CompletedTask;
        }

        public Task<long> PlaceBuyLimitOrderAsync(string currency, decimal limitPrice, decimal quantity, DateTime currentTime)
        {
            long orderId = Interlocked.Increment(ref m_currentOrderId);
            m_openOrders.Add(new OrderIdsAndPrice(orderId, limitPrice));
            return Task.FromResult(orderId);
        }

        public Task CancelOrderAsync(string currency, long orderId)
        {
            m_openOrders.RemoveAll(m=> m.OrderId.Equals(orderId));
            return Task.CompletedTask;
        }

        public Task<string> GetOrderStatusAsync(string currency, long orderId, DateTime currentTime)
        {
            decimal buyPrice = m_openOrders.Where(m => m.OrderId.Equals(orderId)).Select(m => m.BuyPrice).First();
            if (buyPrice > m_candlesProvider.GetLastCandle(currency, 1, currentTime.AddMinutes(2)).Low)
            {
                return Task.FromResult("Filled");
            }
            
            return Task.FromResult("New");
        }
        
        private readonly struct OrderIdsAndPrice
        {
            internal OrderIdsAndPrice(long orderId, decimal buyPrice)
            {
                OrderId = orderId;
                BuyPrice = buyPrice;
            }

            internal long OrderId { get; }
            internal decimal BuyPrice { get; }
        }
    }
}