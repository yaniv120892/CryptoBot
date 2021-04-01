using System;
using System.Threading.Tasks;
using CryptoBot.Abstractions;
using Services.Abstractions;

namespace CryptoBot.CryptoTraders
{
    public class LimitBuyCryptoTrader : IBuyCryptoTrader
    {
        private readonly ITradeService m_tradeService;

        public LimitBuyCryptoTrader(ITradeService tradeService)
        {
            m_tradeService = tradeService;
        }

        public Task<long> BuyAsync(string currency, decimal price, decimal quantity, DateTime currentTime) => 
            m_tradeService.PlaceBuyLimitOrderAsync(currency, price, quantity, currentTime);
    }
}