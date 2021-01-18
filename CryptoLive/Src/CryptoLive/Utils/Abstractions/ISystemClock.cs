using System;
using System.Threading;
using System.Threading.Tasks;

namespace Utils.Abstractions
{
    public interface ISystemClock
    {
        Task<DateTime> Wait(CancellationToken cancellationToken, string desiredSymbol, int timeToWaitInSeconds, string action, DateTime currentTime);
    }
}