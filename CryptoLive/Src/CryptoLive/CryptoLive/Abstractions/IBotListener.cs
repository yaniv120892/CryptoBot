using Common;

namespace CryptoLive.Abstractions
{
    public interface IBotListener
    {
        void Start();
        void AddResults(string currency, BotResultDetails botResultDetails);
        void Stop();
    }
}