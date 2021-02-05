using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Abstractions;

namespace CryptoBot.Abstractions
{
    public interface ICryptoPolling
    {
        Task<IPollingResponse> Start(string symbol, CancellationToken cancellationToken, DateTime currentTime);
    }
}