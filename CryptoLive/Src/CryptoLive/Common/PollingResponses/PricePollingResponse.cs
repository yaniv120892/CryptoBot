using System;
using Common.Abstractions;

namespace Common.PollingResponses
{
    public class PricePollingResponse : IPollingResponse
    {
        public PricePollingResponse(decimal initialPrice, decimal currentPrice, DateTime time)
        {
            InitialPrice = initialPrice;
            CurrentPrice = currentPrice;
            Time = time;
        }

        public decimal InitialPrice { get; }
        public decimal CurrentPrice { get; }
        public DateTime Time { get; }

        public override string ToString()
        {
            return $"InitialPrice: {InitialPrice}, Price {CurrentPrice}, Time {Time}";
        }
    }
}