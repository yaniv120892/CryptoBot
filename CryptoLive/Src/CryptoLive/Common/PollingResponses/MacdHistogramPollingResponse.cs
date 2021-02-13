using System;
using Common.Abstractions;

namespace Common.PollingResponses
{
    public class MacdHistogramPollingResponse : PollingResponseBase, IEquatable<MacdHistogramPollingResponse>
    {
        public MacdHistogramPollingResponse(DateTime time,
            decimal macdHistogram,
            bool isReachMaxTimeInMinutes=false,
            bool isCancelled=false,
            Exception gotException=null)
            : base(time, isCancelled, gotException)
        {
            MacdHistogram = macdHistogram;
            IsReachMaxTimeInMinutes = isReachMaxTimeInMinutes;
        }

        public decimal MacdHistogram { get; }
        public bool IsReachMaxTimeInMinutes { get; }

        public override string ToString()
        {
            return $"IsReachMaxTimeInMinutes: {IsReachMaxTimeInMinutes}, MacdHistogram: {MacdHistogram}, Time: {Time}";
        }

        public bool Equals(MacdHistogramPollingResponse other)
        {
            if (other is null)
            {
                return false;
            }

            return base.Equals(other) &&
                   MacdHistogram == other.MacdHistogram &&
                   IsReachMaxTimeInMinutes == other.IsReachMaxTimeInMinutes;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MacdHistogramPollingResponse);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), MacdHistogram, IsReachMaxTimeInMinutes);
        }
    }
}