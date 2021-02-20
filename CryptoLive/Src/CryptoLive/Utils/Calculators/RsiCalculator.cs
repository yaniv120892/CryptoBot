using System;

namespace Utils.Calculators
{
    public class RsiCalculator
    {
        public static decimal Calculate(decimal upAvg, decimal downAvg)
        {
            if (downAvg == 0)
            {
                return 50;
            }
            decimal rs = Math.Round(upAvg / downAvg ,6);
            decimal rsi = 100 - (100 / (1 + rs));
            return Math.Round(rsi, 6);
        }
    }
}