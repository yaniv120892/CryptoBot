using System.Threading.Tasks;
using CryptoBot.Abstractions;
using Services.Abstractions;

namespace CryptoBot
{
    public class AccountQuoteProvider : IAccountQuoteProvider
    {
        private static readonly int s_percentToUse = 50;
        
        private readonly IAccountService m_accountService;

        public AccountQuoteProvider(IAccountService accountService)
        {
            m_accountService = accountService;
        }

        public async Task<decimal> GetAvailableQuote()
        {
            decimal availableUsdt = await m_accountService.GetAvailableUsdt();
            return s_percentToUse * availableUsdt;
        }
    }
}