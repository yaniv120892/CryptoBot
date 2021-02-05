using System;
using System.Threading;
using System.Threading.Tasks;
using Infra;
using Microsoft.Extensions.Logging;
using Utils.Abstractions;

namespace Utils.SystemClocks
{
    public class DummySystemClock : ISystemClock
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<DummySystemClock>();

        public async Task<DateTime> Wait(CancellationToken cancellationToken, string desiredSymbol, int timeToWaitInSeconds, string action,
            DateTime currentTime)
        {
            await Task.Delay(1 * timeToWaitInSeconds , cancellationToken);
            s_logger.LogTrace($"{desiredSymbol}_{action}: dummy wait for next candle {timeToWaitInSeconds} seconds");
            return currentTime.AddSeconds(timeToWaitInSeconds);
        }
    }
}