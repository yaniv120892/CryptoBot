using System;
using System.Threading.Tasks;
using Services.Abstractions;
using Storage.Abstractions;
using Storage.Abstractions.Providers;

namespace Storage.Providers
{
    public class LivePriceProvider : IPriceProvider
    {
        private readonly IPriceService m_priceService;

        public LivePriceProvider(IPriceService priceService)
        {
            m_priceService = priceService;
        }

        public Task<decimal> GetPrice(string currency, DateTime currentTime)
        {
            return m_priceService.GetPrice(currency, currentTime);
        }
    }
}