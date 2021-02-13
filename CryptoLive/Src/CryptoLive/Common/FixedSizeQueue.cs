using System.Collections.Concurrent;

namespace Common
{
    public class FixedSizeQueue<T>
    {
        public ConcurrentQueue<T> MyQueue = new ConcurrentQueue<T>();

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
}