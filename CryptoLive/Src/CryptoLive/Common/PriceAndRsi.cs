namespace Common
{
    public class PriceAndRsi
    {
        public PriceAndRsi(decimal price, decimal rsi)
        {
            Price = price;
            Rsi = rsi;
        }

        public decimal Price { get; }
        public decimal Rsi { get;}

        public override string ToString()
        {
            return $"Price: {Price}, Rsi {Rsi}";
        }
    }
}