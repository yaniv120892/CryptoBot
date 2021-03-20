using System;
using System.Linq;
using Common.Abstractions;

namespace Common.CryptoQueue
{
    public class CryptoFixedSizeQueueImpl<TPriceAndRsi> : ICryptoPriceAndRsiQueue<TPriceAndRsi>
        where TPriceAndRsi : PriceAndRsi
    {
        private static readonly decimal s_factor = (decimal)1.02;
        private readonly FixedSizeQueue<TPriceAndRsi> m_fixedSizeQueue;
        private readonly int m_size;

        public CryptoFixedSizeQueueImpl(int size)
        {
            m_size = size;
            m_fixedSizeQueue = new FixedSizeQueue<TPriceAndRsi>(size);
        }

        public void Enqueue(TPriceAndRsi currentPriceAndRsi)
        {
            m_fixedSizeQueue.Enqueue(currentPriceAndRsi);
        }

        public TPriceAndRsi GetLowerRsiAndHigherPrice(TPriceAndRsi priceAndRsi)
        {
            return m_fixedSizeQueue.MyQueue.FirstOrDefault(oldRsiAndPrice =>
                IsLowerRsiAndHigherPrice(oldRsiAndPrice, priceAndRsi)
                && oldRsiAndPrice.CandleTime < priceAndRsi.CandleTime.Subtract(TimeSpan.FromMinutes(30))
            );
        }

        public ICryptoPriceAndRsiQueue<TPriceAndRsi> Clone()
        {
            var cloneQueue = new CryptoFixedSizeQueueImpl<TPriceAndRsi>(m_size);
            var arr = new TPriceAndRsi[m_size];
            m_fixedSizeQueue.MyQueue.CopyTo(arr, 0);
            int i = 0;
            while (i < arr.Length && arr[i] != null)
            {
                cloneQueue.Enqueue(arr[i]);
                i++;
            }
            return cloneQueue;
        }

        private static bool IsLowerRsiAndHigherPrice(TPriceAndRsi oldRsiAndPrice, TPriceAndRsi newPriceAndRsi) =>
           newPriceAndRsi.Rsi > oldRsiAndPrice.Rsi && newPriceAndRsi.Price * s_factor < oldRsiAndPrice.Price;
    }
}