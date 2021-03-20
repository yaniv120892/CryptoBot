using System.Threading.Tasks;
using Services.Abstractions;

namespace DemoCryptoLive
{
    public class DemoAccountService : IAccountService
    {
        public Task<decimal> GetAvailableUsdt()
        {
            return Task.FromResult((decimal)100);
        }
    }
}