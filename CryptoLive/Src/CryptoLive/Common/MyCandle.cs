using System;

namespace Common
{
    public class MyCandle
    {
        public decimal Open { get; set; }
        public decimal Close { get; set; }
        public DateTime OpenTime { get; set; }
        public DateTime CloseTime { get; set; }
        public decimal Low { get; set; }
        public decimal High { get; set; }

        public MyCandle()
        {
            
        }
        
        public MyCandle(decimal open, decimal close, DateTime openTime, DateTime closeTime ,decimal low, decimal high)
        {
            Open = open;
            Close = close;
            OpenTime = openTime;
            CloseTime = closeTime;
            Low = low;
            High = high;
        }
    }
}