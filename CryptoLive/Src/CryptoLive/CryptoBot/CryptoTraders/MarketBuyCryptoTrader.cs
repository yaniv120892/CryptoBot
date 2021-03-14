using System;
using System.Threading.Tasks;
using CryptoBot.Abstractions;
using Services.Abstractions;

namespace CryptoBot.CryptoTraders
{
    public class MarketBuyCryptoTrader : IBuyCryptoTrader
    {
        private readonly ITradeService m_tradeService;

        public MarketBuyCryptoTrader(ITradeService tradeService)
        {
            m_tradeService = tradeService;
        }

        public Task<(decimal buyPrice, decimal quantity)> Buy(string currency, decimal quoteOrderQuantity, DateTime currentTime) => 
            m_tradeService.PlaceBuyMarketOrderAsync(currency, quoteOrderQuantity, currentTime);
    }
}