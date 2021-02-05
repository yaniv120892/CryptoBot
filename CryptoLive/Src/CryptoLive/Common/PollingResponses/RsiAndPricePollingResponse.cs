using System;
using Common.Abstractions;

namespace Common.PollingResponses
{
    public class RsiAndPricePollingResponse : IPollingResponse
    {
        public DateTime Time { get; }
        public PriceAndRsi PriceAndRsi { get; }

        public RsiAndPricePollingResponse(DateTime dateTime, PriceAndRsi priceAndRsi)
        {
            Time = dateTime;
            PriceAndRsi = priceAndRsi;
        }
        
        public override string ToString()
        {
            return $"{PriceAndRsi}, Time: {Time}";
        }
    }
}