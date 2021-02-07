using System;
using System.Linq;

namespace Common
{
    public class CryptoFixedSizeQueueImpl<TPriceAndRsi> : FixedSizeQueue<TPriceAndRsi> where TPriceAndRsi: PriceAndRsi
    {
        public TPriceAndRsi GetLowerRsiAndHigherPrice(TPriceAndRsi priceAndRsi)
        {
            return MyQueue.FirstOrDefault(oldRsiAndPrice => 
                priceAndRsi.Rsi > oldRsiAndPrice.Rsi
                && priceAndRsi.Price < oldRsiAndPrice.Price
                && oldRsiAndPrice.CandleTime < priceAndRsi.CandleTime.Subtract(TimeSpan.FromMinutes(15))
            );
        } 
            

        public CryptoFixedSizeQueueImpl(int size) : base(size)
        {
        }
    }
}