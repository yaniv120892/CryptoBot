using System;

namespace Common.Abstractions
{
    public abstract class PollingResponseBase : IEquatable<PollingResponseBase>
    {
        protected PollingResponseBase(DateTime time, bool isCancelled, Exception exception)
        {
            Exception = exception;
            Time = time;
            IsCancelled = isCancelled;
        }

        public DateTime Time { get; }
        public bool IsCancelled { get; }
        public Exception Exception { get; }
        public bool IsSuccess => !IsCancelled && Exception is null;

        public bool Equals(PollingResponseBase other)
        {
            if (other is null)
            {
                return false;
            }
            
            return Time.Equals(other.Time) &&
                   IsCancelled == other.IsCancelled &&
                   Equals(Exception, other.Exception);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PollingResponseBase);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Time, IsCancelled, Exception);
        }

        public override string ToString()
        {
            return $"Time: {Time:dd/MM/yyyy HH:mm:ss}";
        }
    }
}