using System;
using Common.Abstractions;

namespace Common.PollingResponses
{
    public class CandlePollingResponse : IPollingResponse
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
    }
}