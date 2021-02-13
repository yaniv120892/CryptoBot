using System;

namespace Common.Abstractions
{
    public abstract class PollingResponseBase
    {
        protected PollingResponseBase(DateTime time, bool isCancelled)
        {
            Time = time;
            IsCancelled = isCancelled;
        }

        public DateTime Time { get; }
        public bool IsCancelled { get; }
    }
}