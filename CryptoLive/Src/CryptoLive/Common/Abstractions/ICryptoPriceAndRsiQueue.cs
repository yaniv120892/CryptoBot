namespace Common.Abstractions
{
    public interface ICryptoPriceAndRsiQueue<T>
    {
        void Enqueue(T currentPriceAndRsi);
        T GetLowerRsiAndHigherPrice(T priceAndRsi);
    }
}