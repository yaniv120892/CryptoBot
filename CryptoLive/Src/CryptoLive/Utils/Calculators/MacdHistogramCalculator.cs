using System;

namespace Utils.Calculators
{
    public class MacdHistogramCalculator
    {
        public static decimal Calculate(decimal fastEma, decimal slowEma, decimal signal)
        {
            decimal difference = fastEma - slowEma;
            decimal macd = difference - signal;
            return Math.Round(macd, 3);

        }
    }
}
    