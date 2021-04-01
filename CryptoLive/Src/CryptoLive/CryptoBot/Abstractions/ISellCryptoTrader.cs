using System.Threading.Tasks;

namespace CryptoBot.Abstractions
{
    public interface ISellCryptoTrader
    {
        Task SellAsync(string currency, decimal quantity, decimal sellPrice, decimal stopAndLimitPrice);
    }
}