using System;
using System.Threading.Tasks;
using Binance.Net;
using Services.Abstractions;
using Utils.Abstractions;

namespace Services
{
    public class BinancePriceService : IPriceService
    {
        private readonly ICurrencyClientFactory m_currencyClientFactory;

        public BinancePriceService(ICurrencyClientFactory currencyClientFactory)
        {
            m_currencyClientFactory = currencyClientFactory;
        }

        public async Task<decimal> GetPrice(string symbol, DateTime currentTime)
        {
            BinanceClient client = m_currencyClientFactory.Create();
            var response = await client.Spot.Market.GetPriceAsync(symbol);
            decimal currentPrice = response.Data.Price;
            return currentPrice;        
        }
    }
}