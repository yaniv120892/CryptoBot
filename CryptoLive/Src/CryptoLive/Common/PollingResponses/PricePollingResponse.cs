using System;
using Common.Abstractions;

namespace Common.PollingResponses
{
    public class PricePollingResponse : PollingResponseBase
    {
        public PricePollingResponse(decimal initialPrice, 
            decimal currentPrice, 
            DateTime time,
            bool isCancelled=false,
            Exception gotException=null)
            : base(time, isCancelled, gotException)
        {
            InitialPrice = initialPrice;
            CurrentPrice = currentPrice;
        }

        public decimal InitialPrice { get; }
        public decimal CurrentPrice { get; }

        public override string ToString()
        {
            return $"InitialPrice: {InitialPrice}, Price {CurrentPrice}, Time {Time}";
        }
    }
}