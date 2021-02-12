using System;
using Common.Abstractions;

namespace Common.PollingResponses
{
    public class CandlePollingResponse : IPollingResponse, IEquatable<CandlePollingResponse>
    {
        public bool IsBelow { get; }
        public bool IsAbove { get; }
        public DateTime Time { get; }
        public MyCandle Candle { get; }


        public CandlePollingResponse(bool isBelow, 
            bool isAbove, 
            DateTime time, 
            MyCandle candle)
        {
            IsBelow = isBelow;
            IsAbove = isAbove;
            Time = time;
            Candle = candle;
        }
        
        public bool IsWin => IsAbove;

        public override string ToString()
        {
            return $"IsAbove {IsAbove}, IsBelow {IsBelow}, Time {Time}";
        }

        public bool Equals(CandlePollingResponse other)
        {
            if (other is null)
            {
                return false;
            }
            return IsBelow == other.IsBelow &&
                   IsAbove == other.IsAbove &&
                   Time.Equals(other.Time) &&
                   Equals(Candle, other.Candle);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CandlePollingResponse);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(IsBelow, IsAbove, Time, Candle);
        }
    }
}