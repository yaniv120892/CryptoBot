using System;

namespace Utils.Calculators
{
    public class RsiCalculator
    {
        public static decimal Calculate(decimal upAvg, decimal downAvg)
        {
            decimal rs = Math.Round(upAvg / downAvg ,2);
            decimal rsi = 100 - (100 / (1 + rs));
            return rsi;
        }
    }
}