using System;
using System.Linq;
using Common.Abstractions;

namespace Common.CryptoQueue
{
    public class CryptoFixedSizeQueueImpl<TPriceAndRsi> : ICryptoPriceAndRsiQueue<TPriceAndRsi>  where TPriceAndRsi: PriceAndRsi
    {
        private readonly FixedSizeQueue<TPriceAndRsi> m_fixedSizeQueue;
        
        public CryptoFixedSizeQueueImpl(int size)
        {
            m_fixedSizeQueue = new FixedSizeQueue<TPriceAndRsi>(size);
        }
        
        public void Enqueue(TPriceAndRsi currentPriceAndRsi)
        {
            m_fixedSizeQueue.Enqueue(currentPriceAndRsi);
        }

        public TPriceAndRsi GetLowerRsiAndHigherPrice(TPriceAndRsi priceAndRsi)
        {
            return m_fixedSizeQueue.MyQueue.FirstOrDefault(oldRsiAndPrice => 
                priceAndRsi.Rsi > oldRsiAndPrice.Rsi
                && priceAndRsi.Price < oldRsiAndPrice.Price
                //&& priceAndRsi.Price * (decimal) 1.02 < oldRsiAndPrice.Price
                 && oldRsiAndPrice.CandleTime < priceAndRsi.CandleTime.Subtract(TimeSpan.FromMinutes(120))
            );
        }
    }
}