using System;
using System.Threading;
using System.Threading.Tasks;
using Infra;
using Microsoft.Extensions.Logging;
using Utils.Abstractions;

namespace Utils.SystemClocks
{
    public class SystemClock : ISystemClock
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<SystemClock>();

        public async Task<DateTime> Wait(CancellationToken cancellationToken, string desiredSymbol, int timeToWaitInSeconds, string action,
            DateTime currentTime)
        {
            s_logger.LogTrace($"{desiredSymbol}_{action}: Wait for next candle {timeToWaitInSeconds} seconds");
            await Task.Delay(1000 * timeToWaitInSeconds, cancellationToken);
            DateTime dateTime = DateTime.UtcNow;
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute,0);
        }
    }
}