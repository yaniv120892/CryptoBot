using System.Threading.Tasks;
using CryptoBot.Abstractions;
using Services.Abstractions;

namespace CryptoBot
{
    public class AccountQuoteProvider : IAccountQuoteProvider
    {
        private static readonly double s_percentToUse = 0.5;
        
        private readonly IAccountService m_accountService;

        public AccountQuoteProvider(IAccountService accountService)
        {
            m_accountService = accountService;
        }

        public async Task<decimal> GetAvailableQuote()
        {
            decimal availableUsdt = await m_accountService.GetAvailableUsdt();
            return (decimal) ((double)availableUsdt * s_percentToUse);
        }
    }
}