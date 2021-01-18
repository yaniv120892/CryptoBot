using System;
using Common;
using Infra;
using Microsoft.Extensions.Logging;

namespace Utils
{
    public class RsiCalculator
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<RsiCalculator>();
        
        public static decimal Calculate(MyCandle[] openCloseDescription)
        {
            decimal positiveSum = 0;
            decimal negativeSum = 0;
            int amountOfKline = openCloseDescription.Length;
            int upCandles = 0;
            int downCandles = 0;
            decimal[] up = new decimal[amountOfKline - 1];
            decimal[] down = new decimal[amountOfKline - 1];
            for (int i = 1; i < amountOfKline; i++)
            {
                var gainOrLossAmount = (openCloseDescription[i].Close - openCloseDescription[i-1].Close);
                if (gainOrLossAmount > 0)
                {
                    up[i-1] = gainOrLossAmount;
                    down[i-1] = 0;
                    upCandles++;
                }
                else
                {
                    up[i-1] = 0;
                    down[i-1] = -gainOrLossAmount;
                    downCandles++;
                }
            }

            decimal alpha = 1;
            decimal pivot = 1 - 1 / amountOfKline;
            for (int i = up.Length - 1; i >= 0; i--)
            {
                negativeSum += down[i] * alpha;
                positiveSum += up[i] * alpha;
                alpha *= pivot;
            }
            
            decimal rs = Math.Round(positiveSum / negativeSum,2);
            decimal ans = 100 - (100 / (1 + rs));
            s_logger.LogDebug($"Math.Round({positiveSum} / {negativeSum},2)={rs}--------- " +
                              $"100 - (100 / (1 + {rs}))={ans}------------up:{upCandles},down:{downCandles}");
            return ans;
        }
    }
}