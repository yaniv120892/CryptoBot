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
                IsLowerRsiAndHigherPrice(oldRsiAndPrice, priceAndRsi)
                && oldRsiAndPrice.CandleTime < priceAndRsi.CandleTime.Subtract(TimeSpan.FromMinutes(30))
            );
        }

        private static bool IsLowerRsiAndHigherPrice(TPriceAndRsi oldRsiAndPrice, TPriceAndRsi newPriceAndRsi) =>
           newPriceAndRsi.Rsi > oldRsiAndPrice.Rsi && newPriceAndRsi.Price * s_factor < oldRsiAndPrice.Price;
    }
}