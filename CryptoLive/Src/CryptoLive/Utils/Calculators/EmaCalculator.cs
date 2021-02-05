namespace Utils.Calculators
{
    public class EmaCalculator
    {
        public static decimal Calculate(decimal valueToAdd, decimal previousEma, int emaSize)
        {
            return (valueToAdd - previousEma) * (decimal) (2d / (emaSize + 1)) + previousEma;
        }
    }
}