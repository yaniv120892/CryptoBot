namespace Utils.Calculators
{
    public class WsmaCalculator
    {
        public static decimal Calculate(decimal value, decimal previousWsma, int candlesAmount) => 
            previousWsma * (1 - 1 / (decimal) candlesAmount) + (1 / (decimal) candlesAmount) * value;
    }
}