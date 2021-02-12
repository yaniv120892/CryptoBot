using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Abstractions;

namespace CryptoBot.Abstractions
{
    public interface ICryptoPolling
    {
        Task<IPollingResponse> StartAsync(string currency, CancellationToken cancellationToken, DateTime currentTime);
    }
}