using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common.Abstractions;

namespace CryptoBot.Abstractions
{
    public interface ICurrencyBotPhasesExecutor
    {
        Task<PollingResponseBase> WaitUntilRsiIsBelowMaxValueAsync(DateTime currentTime,
            CancellationToken cancellationToken,
            string currency,
            int age,
            int phaseNumber,
            List<string> phasesDescription);
        
        Task<PollingResponseBase> WaitUntilLowerPriceAndHigherRsiAsync(DateTime currentTime,
            CancellationToken cancellationToken,
            string currency,
            int age,
            int phaseNumber,
            List<string> phasesDescription);
        
        Task<(bool, PollingResponseBase)> WaitUnitPriceChangeAsync(DateTime currentTime,
            CancellationToken cancellationToken,
            string currency,
            decimal basePrice,
            int age,
            int phaseNumber,
            List<string> phasesDescription);
        
        bool ValidateCandleIsRed(DateTime currentTime,
            string currency,
            int age,
            int phaseNumber,
            List<string> phasesDescription);
        
        bool ValidateCandleIsGreen(DateTime currentTime,
            string currency,
            int age,
            int phaseNumber, List<string> phasesDescription);
        
        Task<DateTime> WaitAsync(DateTime currentTime,
            CancellationToken cancellationToken,
            string currency,
            int timeToWaitInSeconds,
            string action);
        
        decimal GetPrice(string currency,
            DateTime currentTime);
        
    }
}