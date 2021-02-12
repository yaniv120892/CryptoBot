using System;
using System.Threading;
using System.Threading.Tasks;
using Utils.Abstractions;

namespace Utils.SystemClocks
{
    public class SystemClockWithDelay : ISystemClock
    {
        private readonly ISystemClock m_systemClock;
        private readonly int m_delayTime;

        public SystemClockWithDelay(ISystemClock systemClock,int delayTime)
        {
            m_systemClock = systemClock;
            m_delayTime = delayTime;
        }

        public async Task<DateTime> Wait(CancellationToken cancellationToken, string currency, int timeToWaitInSeconds, string action,
            DateTime currentTime)
        {
            DateTime time = await m_systemClock.Wait(cancellationToken, currency, timeToWaitInSeconds, action, currentTime);
            return time.Subtract(TimeSpan.FromMinutes(m_delayTime));
        }
    }
}