using System;
using Common;
using Infra;
using Microsoft.Extensions.Logging;

namespace Utils.Calculators
{
    public class SmaCalculator
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<SmaCalculator>();

        public static decimal CalculateSmaHigh(Span<MyCandle> openCloseDescription)
        {
            decimal sum = 0;
            foreach (MyCandle myCandle in openCloseDescription)
            {
                sum += myCandle.High;
            }

            return sum / (openCloseDescription.Length);
        }
        
        public static decimal CalculateSma(Span<MyCandle> openCloseDescription)
        {
            decimal sum = 0;
            foreach (MyCandle myCandle in openCloseDescription)
            {
                sum += myCandle.Close;
            }

            return sum / (openCloseDescription.Length);
        }
    }
}