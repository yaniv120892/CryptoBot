using System;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.PollingResponses;

namespace Utils.Abstractions
{
    public interface IPolling
    {
        Task<IPollingResponse> StartPolling(string desiredSymbol, CancellationToken cancellationToken, DateTime currentTime);
    }
}