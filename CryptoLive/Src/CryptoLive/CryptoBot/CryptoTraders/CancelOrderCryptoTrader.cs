using System.Threading.Tasks;
using CryptoBot.Abstractions;
using Services.Abstractions;

namespace CryptoBot.CryptoTraders
{
    public class CancelOrderCryptoTrader : ICancelOrderCryptoTrader
    {
        private readonly ITradeService m_tradeService;

        public CancelOrderCryptoTrader(ITradeService tradeService)
        {
            m_tradeService = tradeService;
        }

        public Task CancelAsync(string currency, long order) => m_tradeService.CancelOrderAsync(currency, order);
    }
}