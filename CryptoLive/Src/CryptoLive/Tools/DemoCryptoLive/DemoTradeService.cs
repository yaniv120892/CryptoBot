using System;
using System.Threading.Tasks;
using Services.Abstractions;

namespace DemoCryptoLive
{
    internal class DemoTradeService : ITradeService
    {
        private readonly IPriceService m_priceService;

        public DemoTradeService(IPriceService priceService)
        {
            m_priceService = priceService;
        }

        public async Task<(decimal buyPrice, decimal quantity)> PlaceBuyMarketOrderAsync(string currency, decimal quoteOrderQuantity, DateTime currentTime)
        {
            decimal buyPrice = await m_priceService.GetPrice(currency, currentTime);
            decimal quantity = quoteOrderQuantity / buyPrice;
            return (buyPrice, quantity);
        }

        public Task PlaceSellOcoOrderAsync(string currency, decimal quantity, decimal price, decimal stopAndLimitPrice)
        {
            return Task.CompletedTask;
        }
    }
}