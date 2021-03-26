using System;
using System.Collections.Concurrent;
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

        public Task<DateTime> Wait(CancellationToken cancellationToken, string currency, int timeToWaitInSeconds,
            string action,
            DateTime currentTime)
        {
            s_logger.LogTrace($"{currency}_{action}: dummy wait for next candle {timeToWaitInSeconds} seconds");
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException();
            }

            return Task.FromResult(currentTime.AddSeconds(timeToWaitInSeconds));
        }
    }
}