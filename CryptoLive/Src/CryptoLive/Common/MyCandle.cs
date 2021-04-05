using System;
using System.Globalization;

namespace Common
{
    public class MyCandle : IEquatable<MyCandle>
    {
        public decimal Open { get; set; }
        public decimal Close { get; set; }
        public DateTime OpenTime { get; set; }
        public DateTime CloseTime { get; set; }
        public decimal Low { get; set; }
        public decimal High { get; set; }
        public TimeSpan CandleSizeInMinutes => TimeSpan.FromMinutes(CloseTime.Subtract(OpenTime).Minutes+1);

        public MyCandle()
        {
            // For csvHelper
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
        
        public override bool Equals(Object obj)
        {
            //Check for null and compare run-time types.
            if (obj == null)
            {
                return false;
            }

            return Equals(obj as MyCandle);
        }

        public bool Equals(MyCandle other)
        {
            if (other is null)
            {
                return false;
            }
            
            return Open.Equals(other.Open) && 
                   Close.Equals(other.Close) && 
                   OpenTime.Day.Equals(other.OpenTime.Day) && 
                   OpenTime.Hour.Equals(other.OpenTime.Hour) && 
                   OpenTime.Minute.Equals(other.OpenTime.Minute) && 
                   OpenTime.Second.Equals(other.OpenTime.Second) && 
                   CloseTime.Day.Equals(other.CloseTime.Day) && 
                   CloseTime.Hour.Equals(other.CloseTime.Hour) && 
                   CloseTime.Minute.Equals(other.CloseTime.Minute) && 
                   CloseTime.Second.Equals(other.CloseTime.Second) && 
                   Low.Equals(other.Low) && 
                   High.Equals(other.High);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Open, 
                Close, 
                OpenTime.ToString(CultureInfo.InvariantCulture),
                CloseTime.ToString(CultureInfo.InvariantCulture), 
                Low, 
                High);
        }

        public override string ToString()
        {
            return
                $"Open = {Open}, " +
                $"Close = {Close}, " +
                $"OpenTime = {OpenTime:dd/MM/yyyy HH:mm:ss}, " +
                $"CloseTime = {CloseTime:dd/MM/yyyy HH:mm:ss}, " +
                $"Low = {Low}, " +
                $"High = {High}";
        }

        public static MyCandle CloneWithNewTime(MyCandle prevCandle, int minutesToAdd)
        {
            var candle = new MyCandle(prevCandle.Open, prevCandle.Close, 
                prevCandle.OpenTime.AddMinutes(minutesToAdd), prevCandle.CloseTime.AddMinutes(minutesToAdd), 
                prevCandle.Low, prevCandle.High);
            return GetCandleWithFixedSecondsRange(candle);
        }
        
        public static MyCandle GetCandleWithFixedSecondsRange(MyCandle candle)
        {
            if (candle.OpenTime.Second != 0 || candle.CloseTime.Second != 59)
            {
                DateTime alignedOpenTime = AlignDateTimeSeconds(candle.OpenTime, 0);
                DateTime alignedCloseTime = AlignDateTimeSeconds(candle.CloseTime, 59);
                return new MyCandle(candle.Open, candle.Close, 
                    alignedOpenTime, alignedCloseTime, 
                    candle.Low, candle.High);
            }

            return candle;
        }

        private static DateTime AlignDateTimeSeconds(DateTime time, int seconds)
        {
            if (time.Second != seconds)
            {
                return new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute,
                    seconds);
            }
            
            return time;
        }
    }
}