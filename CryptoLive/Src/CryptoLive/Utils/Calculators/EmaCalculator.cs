using System;

namespace Utils.Calculators
{
    public class EmaCalculator
    {
        public static decimal Calculate(decimal valueToAdd, decimal previousEma, int emaSize)
        {
            var ans = (valueToAdd - previousEma) * (decimal) (2d / (emaSize + 1)) + previousEma;
            return Math.Round(ans, 3);
        }
    }
}