using System;
using Common.Abstractions;

namespace Common.PollingResponses
{
    public class OrderPollingResponse : PollingResponseBase
    {
        public OrderPollingResponse(DateTime time,
            long orderId,
            bool isCancelled=false,
            Exception gotException=null)
            : base(time, isCancelled, gotException)
        {
            OrderId = orderId;
        }
        
        public long OrderId { get; }

        public override string ToString() => 
            $"OrderId: {OrderId}";
    }
}