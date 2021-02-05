using System.Collections.Concurrent;
using System.Linq;

namespace Common
{
    public class FixedSizeQueue<T>
    {
        protected ConcurrentQueue<T> MyQueue = new ConcurrentQueue<T>();

        public int Size { get; }

        public FixedSizeQueue(int size)
        {
            Size = size;
        }

        public void Enqueue(T obj)
        {
            MyQueue.Enqueue(obj);

            while (MyQueue.Count > Size)
            {
                MyQueue.TryDequeue(out T _);
            }
        }

    }

    public class CryptoFixedSizeQueueImpl<TPriceAndRsi> : FixedSizeQueue<TPriceAndRsi> where TPriceAndRsi: PriceAndRsi
    {
        public TPriceAndRsi GetLowerRsiAndHigherPrice(TPriceAndRsi priceAndRsi) => 
            MyQueue.FirstOrDefault(oldRsiAndPrice => 
                priceAndRsi.Rsi > oldRsiAndPrice.Rsi
                && priceAndRsi.Price < oldRsiAndPrice.Price);

        public CryptoFixedSizeQueueImpl(int size) : base(size)
        {
        }
    }
}