using System;
using System.Threading;
using System.Threading.Tasks;
using Infra;
using Microsoft.Extensions.Logging;
using Utils.Abstractions;

namespace Utils
{
    public class SystemClock : ISystemClock
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<SystemClock>();

        public async Task<DateTime> Wait(CancellationToken cancellationToken, string desiredSymbol, int timeToWaitInSeconds, string action,
            DateTime currentTime)
        {
            s_logger.LogDebug($"{desiredSymbol}_{action}: Wait for next candle {timeToWaitInSeconds} seconds");
            await Task.Delay(1000 * timeToWaitInSeconds, cancellationToken);
            return DateTime.Now;
        }
    }
}