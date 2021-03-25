using System;
using System.Threading.Tasks;
using Binance.Net;
using Services.Abstractions;

namespace Services
{
    public class BinancePriceService : IPriceService
    {
        private readonly ICurrencyClientFactory m_currencyClientFactory;

        public BinancePriceService(ICurrencyClientFactory currencyClientFactory)
        {
            m_currencyClientFactory = currencyClientFactory;
        }

        public async Task<decimal> GetPrice(string currency, DateTime currentTime)
        {
            BinanceClient client = m_currencyClientFactory.Create();
            var response = await HttpRequestRetryHandler.RetryOnFailure(
                async () => await client.Spot.Market.GetPriceAsync(currency),
                "Get Price");
            decimal currentPrice = response.Data.Price;
            return currentPrice;        
        }
    }
}