using System;
using Common.Abstractions;

namespace Common.PollingResponses
{
    public class RsiAndPricePollingResponse : IPollingResponse
    {
        public DateTime Time { get; }
        public PriceAndRsi NewPriceAndRsi { get; }
        public PriceAndRsi OldPriceAndRsi { get; }

        public RsiAndPricePollingResponse(DateTime dateTime, 
            PriceAndRsi oldPriceAndRsi,
            PriceAndRsi newPriceAndRsi)
        {
            Time = dateTime;
            NewPriceAndRsi = newPriceAndRsi;
            OldPriceAndRsi = oldPriceAndRsi;
        }
        
        public override string ToString()
        {
            return $"{NewPriceAndRsi}, Time: {Time}";
        }
    }
}