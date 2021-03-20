using System.Threading.Tasks;

namespace CryptoBot.Abstractions
{
    public interface IAccountQuoteProvider
    {
        Task<decimal> GetAvailableQuote();
    }
}