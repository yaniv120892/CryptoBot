using System;
using Common.Abstractions;

namespace Common.PollingResponses
{
    public class CandlePollingResponse : IPollingResponse
    {
        public bool IsBelow { get; }
        public bool IsAbove { get; }
        public DateTime Time { get; }

        public CandlePollingResponse(in bool isBelow, in bool isAbove, DateTime time)
        {
            IsBelow = isBelow;
            IsAbove = isAbove;
            Time = time;
        }
        
        public bool IsGain => IsAbove;

        public override string ToString()
        {
            return $"IsAbove {IsAbove}, IsBelow {IsBelow}, Time {Time}";
        }
    }
}