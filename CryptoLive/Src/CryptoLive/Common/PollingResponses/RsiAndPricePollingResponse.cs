using System;
using Common.Abstractions;

namespace Common.PollingResponses
{
    public class RsiAndPricePollingResponse : PollingResponseBase
    {
        public PriceAndRsi NewPriceAndRsi { get; }
        public PriceAndRsi OldPriceAndRsi { get; }

        public RsiAndPricePollingResponse(DateTime time, 
            PriceAndRsi oldPriceAndRsi,
            PriceAndRsi newPriceAndRsi,
            bool isCancelled=false) 
            : base(time, isCancelled)
        {
            NewPriceAndRsi = newPriceAndRsi;
            OldPriceAndRsi = oldPriceAndRsi;
        }
        
        public override string ToString()
        {
            return $"NewPriceAndRsi: {NewPriceAndRsi}, OldPriceAndRsi: {OldPriceAndRsi}, Time: {Time}";
        }
    }
}