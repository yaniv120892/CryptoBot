using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Infra;
using Microsoft.Extensions.Logging;

namespace DemoCryptoLive
{
    public class ReportGenerator
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<ReportGenerator>();

        public static async Task GenerateReport(Dictionary<string, Task<(int, int, int, string, decimal)>> tasks, 
            string[] currencies, decimal priceChangeToNotify)
        {
            int totalWinCounter = 0;
            int totalLossCounter = 0;
            int totalEvenCounter = 0;
            int total;
            
            foreach (string currency in currencies)
            {
                (int winCounter, int lossCounter, int evenCounter, string winAndLossesDescription, decimal _) = await tasks[currency];
                s_logger.LogInformation(winAndLossesDescription);
                total = winCounter + lossCounter;
                decimal currencySuccess = CalculateSuccess(winCounter, total);
                double currencyReturn = CalculateReturn(winCounter, lossCounter, priceChangeToNotify);
                s_logger.LogInformation(
                    $"{currency} Summary: " +
                    $"Success: {currencySuccess}%, " +
                    $"Return: {currencyReturn:F2}%, " +
                    $"Win: {winCounter}, " +
                    $"Loss: {lossCounter}, " +
                    $"Even: {evenCounter}, " +
                    $"Total: {total}");
                totalWinCounter += winCounter;
                totalLossCounter += lossCounter;
                totalEvenCounter += evenCounter;
            }

            total = totalWinCounter + totalLossCounter;
            decimal totalSuccess = CalculateSuccess(totalWinCounter, total);
            double totalReturn = CalculateReturn(totalWinCounter, totalLossCounter, priceChangeToNotify);
            s_logger.LogInformation(
                $"Final Summary: " +
                $"Success: {totalSuccess}%, " +
                $"Return: {totalReturn:F2}%, " +
                $"Win: {totalWinCounter}, " +
                $"Loss: {totalLossCounter}, " +
                $"Even: {totalEvenCounter}, " +
                $"Total: {total}");
        }

        private static double CalculateReturn(decimal winCounter,
            decimal lossCounter, decimal priceChangeToNotify)
        {
            double winReturn = Math.Pow((double) ((100 + priceChangeToNotify)/100), (double) winCounter);
            double lossReturn = Math.Pow((double) ((100 - priceChangeToNotify)/100), (double) lossCounter);
            return winReturn * lossReturn;
        }

        private static decimal CalculateSuccess(int winCounter, int winAndLossCounter) => 
            winAndLossCounter == 0 ? 0 : winCounter * 100 / winAndLossCounter;
    }
}