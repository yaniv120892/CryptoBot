using System.Threading.Tasks;

namespace CryptoLive.Abstractions
{
    public interface ITradingSystem
    {
        Task Run();
        void Stop();
    }
}