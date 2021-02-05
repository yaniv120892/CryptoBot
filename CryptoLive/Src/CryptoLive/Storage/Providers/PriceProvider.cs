using System;
using System.Threading.Tasks;
using Services.Abstractions;
using Storage.Abstractions;

namespace Storage.Providers
{
    public class PriceProvider : IPriceProvider
    {
        private readonly IPriceService m_priceService;

        public PriceProvider(IPriceService priceService)
        {
            m_priceService = priceService;
        }

        public Task<decimal> GetPrice(string symbol, DateTime currentTime)
        {
            return m_priceService.GetPrice(symbol, currentTime);
        }
    }
}