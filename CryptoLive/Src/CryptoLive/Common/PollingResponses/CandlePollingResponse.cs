using System;
using Common.Abstractions;

namespace Common.PollingResponses
{
    public class CandlePollingResponse : PollingResponseBase, IEquatable<CandlePollingResponse>
    {
        public bool IsBelow { get; }
        public bool IsAbove { get; }
        public MyCandle Candle { get; }

        public CandlePollingResponse(bool isBelow, 
            bool isAbove, 
            DateTime time, 
            MyCandle candle, 
            bool isCancelled=false,
            Exception gotException=null)
            : base(time, isCancelled, gotException)
        {
            IsBelow = isBelow;
            IsAbove = isAbove;
            Candle = candle;
        }
        
        public bool IsWin => IsAbove;

        public override string ToString()
        {
            return $"Is Above {IsAbove}, Is Below {IsBelow}, Time {Time:dd/MM/yyyy HH:mm:ss}";
        }

        public bool Equals(CandlePollingResponse other)
        {
            if (other is null)
            {
                return false;
            }

            return base.Equals(other) &&
                   IsBelow == other.IsBelow &&
                   IsAbove == other.IsAbove &&
                   Equals(Candle, other.Candle);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CandlePollingResponse);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), IsBelow, IsAbove, Candle);
        }
    }
}