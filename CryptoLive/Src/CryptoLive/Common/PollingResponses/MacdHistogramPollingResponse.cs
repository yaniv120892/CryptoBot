using System;
using Common.Abstractions;

namespace Common.PollingResponses
{
    public class MacdHistogramPollingResponse : IPollingResponse
    {
        public MacdHistogramPollingResponse(DateTime time, decimal macdHistogram)
        {
            Time = time;
            MacdHistogram = macdHistogram;
        }

        public DateTime Time { get; }
        public decimal MacdHistogram { get; }

        public override string ToString()
        {
            return $"MacdHistogram: {MacdHistogram}, Time: {Time}";
        }
    }
}