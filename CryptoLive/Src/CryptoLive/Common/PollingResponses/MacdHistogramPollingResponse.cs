using System;
using Common.Abstractions;

namespace Common.PollingResponses
{
    public class MacdHistogramPollingResponse : PollingResponseBase
    {
        public MacdHistogramPollingResponse(DateTime time,
            decimal macdHistogram,
            bool isCancelled=false) 
            : base(time, isCancelled)
        {
            MacdHistogram = macdHistogram;
        }

        public decimal MacdHistogram { get; }

        public override string ToString()
        {
            return $"MacdHistogram: {MacdHistogram}, Time: {Time}";
        }
    }
}