using System.Threading.Tasks;

namespace Services.Abstractions
{
    public interface IAccountService
    {
        Task<decimal> GetAvailableUsdt();
    }
}