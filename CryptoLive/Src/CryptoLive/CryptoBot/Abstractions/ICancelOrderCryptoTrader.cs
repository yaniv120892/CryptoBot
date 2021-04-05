using System.Threading.Tasks;

namespace CryptoBot.Abstractions
{
    public interface ICancelOrderCryptoTrader
    {
        Task CancelAsync(string currency, long order);
    }
}