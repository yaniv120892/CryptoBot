using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common.Abstractions;
using Common.PollingResponses;
using CryptoBot.Abstractions;
using CryptoBot.CryptoPollings;
using CryptoBot.CryptoValidators;
using Infra;
using Microsoft.Extensions.Logging;

namespace CryptoBot
{
    public class CurrencyBotPhasesExecutor : ICurrencyBotPhasesExecutor
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<CurrencyBotPhasesExecutor>();

        private readonly ICryptoBotPhasesFactory m_cryptoBotPhasesFactory;
        private readonly int m_rsiCandleSize;
        private readonly int m_rsiMemorySize;
        private readonly int m_redCandleSize;
        private readonly int m_greenCandleSize;
        private readonly int m_priceChangeDelayTimeIterationsInSeconds;
        private readonly int m_minutesToWaitBeforePollingPrice;
        private readonly int m_priceChangeCandleSize;
        private readonly decimal m_maxRsiToNotify;
        private readonly decimal m_priceChangeToNotify;

        public CurrencyBotPhasesExecutor(ICryptoBotPhasesFactory cryptoBotPhasesFactory, 
            decimal maxRsiToNotify, 
            int rsiMemorySize, 
            int rsiCandleSize, 
            int redCandleSize, 
            int greenCandleSize, 
            int priceChangeDelayTimeIterationsInSeconds, 
            int minutesToWaitBeforePollingPrice, 
            decimal priceChangeToNotify, 
            int priceChangeCandleSize)
        {
            m_cryptoBotPhasesFactory = cryptoBotPhasesFactory;
            m_maxRsiToNotify = maxRsiToNotify;
            m_rsiMemorySize = rsiMemorySize;
            m_rsiCandleSize = rsiCandleSize;
            m_redCandleSize = redCandleSize;
            m_greenCandleSize = greenCandleSize;
            m_priceChangeDelayTimeIterationsInSeconds = priceChangeDelayTimeIterationsInSeconds;
            m_minutesToWaitBeforePollingPrice = minutesToWaitBeforePollingPrice;
            m_priceChangeToNotify = priceChangeToNotify;
            m_priceChangeCandleSize = priceChangeCandleSize;
        }

        public async Task<DateTime> WaitUntilRsiIsBelowMaxValueAsync(DateTime currentTime,
            CancellationToken cancellationToken,
            string currency,
            int age,
            int phaseNumber, 
            List<string> phasesDescription)
        {
            s_logger.LogInformation($"{currency}_{age}: {currentTime} Start phase {phaseNumber}: wait until RSI is below {m_maxRsiToNotify}");
            RsiCryptoPolling rsiPolling = m_cryptoBotPhasesFactory.CreateRsiPolling(m_maxRsiToNotify);
            PollingResponseBase responseBase = await rsiPolling.StartAsync(currency, cancellationToken, currentTime);
            s_logger.LogInformation($"{currency}_{age}: Done phase 1: RSI is below {m_maxRsiToNotify} {responseBase.Time}");
            phasesDescription.Add($"{phaseNumber}.Wait until RSI is below {m_maxRsiToNotify}, Info :" +
                                  $"Currency:{currency}, StartTime:{currentTime}, EndTime:{responseBase.Time}, Rsi: {((RsiPollingResponse)responseBase).Rsi}");
            return responseBase.Time;
        }
        
        public async Task<DateTime> WaitUntilLowerPriceAndHigherRsiAsync(DateTime currentTime,
            CancellationToken cancellationToken,
            string currency,
            int age,
            int phaseNumber,
            List<string> phasesDescription)
        {
            s_logger.LogInformation($"{currency}_{age}: Start phase {phaseNumber}: wait until lower price and higher RSI is {currentTime}");
            PriceAndRsiCryptoPolling priceAndRsiPolling = m_cryptoBotPhasesFactory.CreatePriceAndRsiPolling(m_rsiCandleSize, m_maxRsiToNotify, m_rsiMemorySize);
            PollingResponseBase responseBase = await priceAndRsiPolling.StartAsync(currency, cancellationToken, currentTime);
            s_logger.LogInformation($"{currency}_{age}: Done phase {phaseNumber}: wait until lower price and higher RSI {responseBase.Time}"); 
            phasesDescription.Add($"{phaseNumber}.Wait until lower price and higher RSI, Info :" +
                                  $"Currency:{currency}, StartTime:{currentTime}, EndTime:{responseBase.Time}, \nNew:{((PriceAndRsiPollingResponse)responseBase).NewPriceAndRsi}, \nOld:{((PriceAndRsiPollingResponse)responseBase).OldPriceAndRsi}");
            return responseBase.Time;
        }
        
        public async Task<(bool, DateTime)> WaitUnitPriceChangeAsync(DateTime currentTime,
            CancellationToken cancellationToken,
            string currency,
            decimal basePrice,
            int age,
            int phaseNumber,
            List<string> phasesDescription)
        {
            s_logger.LogInformation($"{currency}_{age} Start phase {phaseNumber}: get price every {m_priceChangeDelayTimeIterationsInSeconds / 60} minutes until it changed by {m_priceChangeToNotify}%, price: {basePrice}, {currentTime}");
            currentTime = await m_cryptoBotPhasesFactory.SystemClock.Wait(cancellationToken, currency,m_minutesToWaitBeforePollingPrice*60, "FullMode_WaitBeforeStartPricePolling", currentTime);
            CandleCryptoPolling candlePolling = m_cryptoBotPhasesFactory.CreateCandlePolling(basePrice, m_priceChangeDelayTimeIterationsInSeconds, m_priceChangeCandleSize, m_priceChangeToNotify);
            PollingResponseBase responseBase = await candlePolling.StartAsync(currency,cancellationToken, currentTime);
            var candlePollingResponse = AssertIsCandlePollingResponse(responseBase);
            string increaseOrDecreaseStr = candlePollingResponse.IsWin ? "increase by" : "decreased by";
            s_logger.LogInformation($"{currency}_{age} Done phase {phaseNumber}: price {increaseOrDecreaseStr} {m_priceChangeToNotify}%, {candlePollingResponse.Time}");
            phasesDescription.Add($"{phaseNumber}.Wait until price change by {m_priceChangeToNotify}%, Info :" +
                                  $"Currency:{currency}, StartTime:{currentTime}, EndTime:{candlePollingResponse.Time}, BasePrice:{basePrice}, IsWin: {candlePollingResponse.IsWin}, Candle: {candlePollingResponse.Candle}");
            return (candlePollingResponse.IsWin, candlePollingResponse.Time);
        }

        public bool ValidateCandleIsRed(DateTime currentTime,
            string currency,
            int age,
            int phaseNumber, 
            List<string> phasesDescription)
        {
            s_logger.LogInformation($"{currency}_{age} Start phase {phaseNumber}: validate current candle is red {currentTime}");
            RedCandleValidator redCandleValidator = m_cryptoBotPhasesFactory.CreateRedCandleValidator(m_redCandleSize);
            bool isCandleRed = redCandleValidator.Validate(currency, currentTime);
            s_logger.LogInformation($"{currency}_{age} Done phase {phaseNumber}: current candle is red: {isCandleRed} {currentTime}");
            phasesDescription.Add($"{phaseNumber}.Validate candle is red, Info :" +
                                  $"Currency:{currency}, StartTime:{currentTime}, IsRed: {isCandleRed}");
            return isCandleRed;        
        }

        public bool ValidateCandleIsGreen(DateTime currentTime,
            string currency,
            int age,
            int phaseNumber, List<string> phasesDescription)
        {
            s_logger.LogInformation($"{currency}_{age} Start phase {phaseNumber}: validate current candle is green {currentTime}");
            GreenCandleValidator greenCandleValidator = m_cryptoBotPhasesFactory.CreateGreenCandleValidator(m_greenCandleSize);
            bool isCandleGreen = greenCandleValidator.Validate(currency, currentTime);
            s_logger.LogInformation($"{currency}_{age} Done phase {phaseNumber}: current candle is green: {isCandleGreen} {currentTime}");
            phasesDescription.Add($"{phaseNumber}.Validate candle is green, Info :" +
                                  $"Currency:{currency}, StartTime:{currentTime}, IsGreen: {isCandleGreen}");
            return isCandleGreen;
        }

        public Task<DateTime> WaitAsync(DateTime currentTime,
            CancellationToken cancellationToken,
            string currency, 
            int timeToWaitInSeconds,
            string action)
        {
            return m_cryptoBotPhasesFactory.SystemClock.Wait(cancellationToken,currency,timeToWaitInSeconds, action, currentTime);
        }

        public Task<decimal> GetPriceAsync(string currency,
            DateTime currentTime)
        {
            return m_cryptoBotPhasesFactory.CurrencyDataProvider.GetPriceAsync(currency, currentTime);
        }
        
        private static CandlePollingResponse AssertIsCandlePollingResponse(PollingResponseBase pollingResponseBase)
        {
            if (!(pollingResponseBase is CandlePollingResponse candlePollingResponse))
            {
                throw new Exception("candle polling response should be of type CandlePollingResponse");
            }

            return candlePollingResponse;
        }
    }

    public interface ICurrencyBotPhasesExecutor
    {
        Task<DateTime> WaitUntilRsiIsBelowMaxValueAsync(DateTime currentTime,
            CancellationToken cancellationToken,
            string currency,
            int age,
            int phaseNumber,
            List<string> phasesDescription);
        
        Task<DateTime> WaitUntilLowerPriceAndHigherRsiAsync(DateTime currentTime,
            CancellationToken cancellationToken,
            string currency,
            int age,
            int phaseNumber,
            List<string> phasesDescription);
        
        Task<(bool, DateTime)> WaitUnitPriceChangeAsync(DateTime currentTime,
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
        
        Task<decimal> GetPriceAsync(string currency,
            DateTime currentTime);
        
    }
}