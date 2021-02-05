using System;

namespace Utils.Calculators
{
    public class MeanAverageCalculator
    {
        public static decimal Calculate(Span<decimal> numbers)
        {
            decimal sum = 0;
            foreach (var number in numbers)
            {
                sum += number;
            }

            return sum / numbers.Length;
        }
    }
}