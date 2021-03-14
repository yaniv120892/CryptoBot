using System.Threading.Tasks;
using CryptoBot.Abstractions;
using Services.Abstractions;

namespace CryptoBot.CryptoTraders
{
    public class OcoSellCryptoTrader : ISellCryptoTrader
    {
        private readonly ITradeService m_tradeService;

        public OcoSellCryptoTrader(ITradeService tradeService)
        {
            m_tradeService = tradeService;
        }

        public Task PlaceSellOcoOrderAsync(string currency, decimal quantity, decimal sellPrice, decimal stopAndLimitPrice) => 
            m_tradeService.PlaceSellOcoOrderAsync(currency, quantity, sellPrice, stopAndLimitPrice);
    }
}