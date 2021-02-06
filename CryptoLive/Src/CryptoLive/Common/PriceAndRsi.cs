using System;

namespace Common
{
    public class PriceAndRsi
    {
        public PriceAndRsi(decimal price, decimal rsi, DateTime candleTime)
        {
            Price = price;
            Rsi = rsi;
            CandleTime = candleTime;
        }

        public decimal Price { get; }
        public decimal Rsi { get;}
        public DateTime CandleTime { get; }

        public override string ToString()
        {
            return $"Price {Price}, Rsi {Rsi}, CandleTime {CandleTime}";
        }
    }
}