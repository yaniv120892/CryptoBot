using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Abstractions;

namespace CryptoBot.Abstractions
{
    public interface ICurrencyBotPhasesExecutor
    {
        Task<PollingResponseBase> WaitUntilLowerPriceAndHigherRsiAsync(DateTime currentTime,
            CancellationToken cancellationToken,
            string currency,
            int age,
            int phaseNumber,
            List<string> phasesDescription,
            ICryptoPriceAndRsiQueue<PriceAndRsi> cryptoPriceAndRsiQueue,
            Queue<CancellationToken> parentRunningCancellationToken);

        Task<(bool, PollingResponseBase)> WaitUnitPriceChangeAsync(DateTime currentTime,
            CancellationToken cancellationToken,
            string currency,
            decimal minPrice,
            decimal maxPrice,
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
        
        bool ValidatePriceAboveMeanAverage(DateTime currentTime, 
            string currency, 
            int age, 
            int phaseNumber, 
            List<string> phasesDescription);
        bool ValidatePriceBelowMeanAverage(DateTime currentTime, 
            string currency, 
            int age, 
            int phaseNumber, 
            List<string> phasesDescription);
        
        Task<DateTime> WaitAsync(DateTime currentTime,
            CancellationToken cancellationToken,
            string currency,
            int timeToWaitInSeconds,
            string action);

        Task<DateTime> WaitForNextCandleAsync(DateTime currentTime, 
            CancellationToken token, 
            string currency);
        
        Task<BuyAndSellTradeInfo> BuyAndPlaceSellOrder(DateTime currentTime,
            CancellationToken cancellationToken,
            string currency,
            int age,
            int phaseNumber,
            List<string> phasesDescription,
            decimal buyPrice,
            decimal quoteOrderQuantity);

        decimal GetLastRecordedPrice(string currency, DateTime currentTime);
    }
}