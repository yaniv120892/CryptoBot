using System;
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
    public class CurrencyBotPhasesExecutor
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<CurrencyBotPhasesExecutor>();

        private readonly ICryptoBotPhasesFactory m_cryptoBotPhasesFactory;
        private readonly int m_macdCandleSize;
        private readonly int m_maxMacdPollingTime;
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
            int macdCandleSize, 
            int maxMacdPollingTime, 
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
            m_macdCandleSize = macdCandleSize;
            m_maxMacdPollingTime = maxMacdPollingTime;
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

        public async Task<(bool isMacdPositive, DateTime currentTime)> WaitUntilMacdIsPositive(string currency,
            DateTime currentTime, 
            int age, 
            CancellationToken cancellationToken)
        {
            s_logger.LogInformation($"{currency}_{age} Start phase 3: wait until MACD is positive {currentTime}");
            MacdHistogramCryptoPolling macdHistogramPolling = m_cryptoBotPhasesFactory.CreateMacdPolling(m_macdCandleSize, m_maxMacdPollingTime);
            MacdHistogramPollingResponse macdHistogramPollingResponse = (MacdHistogramPollingResponse) await macdHistogramPolling.Start(currency, cancellationToken ,currentTime);
            s_logger.LogInformation($"{currency}_{age} Done phase 3: wait until MACD is positive {macdHistogramPollingResponse.Time} :");
            bool isPositiveMacdHistogram = macdHistogramPollingResponse.MacdHistogram > 0;
            return (isPositiveMacdHistogram, macdHistogramPollingResponse.Time);
        }

        public bool ValidateMacdNegative(string currency, int age, DateTime currentTime)
        {
            s_logger.LogInformation($"{currency}_{age} Start phase 2: validate current macd is negative {currentTime}");
            MacdHistogramNegativeValidator macdHistogramNegativeValidator = m_cryptoBotPhasesFactory.CreateMacdNegativeValidator(m_macdCandleSize);
            bool isCurrentMacdNegative = macdHistogramNegativeValidator.Validate(currency, currentTime);
            s_logger.LogInformation($"{currency}_{age} Done phase 2: validate current macd is negative {isCurrentMacdNegative} {currentTime}");
            return isCurrentMacdNegative;            
        }
        
        public async Task<DateTime> WaitUntilRsiIsBelowMaxValue(string currency, 
            CancellationToken cancellationToken, 
            int age, 
            DateTime currentTime)
        {
            s_logger.LogInformation($"{currency}_{age}: Start phase 1: wait until RSI is below {m_maxRsiToNotify} {currentTime}");
            RsiCryptoPolling rsiPolling = m_cryptoBotPhasesFactory.CreateRsiPolling(m_maxRsiToNotify);
            IPollingResponse response = await rsiPolling.Start(currency, cancellationToken, currentTime);
            s_logger.LogInformation($"{currency}_{age}: Done phase 1: RSI is below {m_maxRsiToNotify} {response.Time}");
            return response.Time;
        }
        
        public async Task<DateTime> WaitUntilLowerPriceAndHigherRsi(string currency, 
            DateTime currentTime,
            CancellationToken cancellationToken, 
            int age)
        {
            s_logger.LogInformation($"{currency}_{age}: Start phase 1: wait until lower price and higher RSI is {currentTime}");
            PriceAndRsiCryptoPolling priceAndRsiPolling = m_cryptoBotPhasesFactory.CreatePriceAndRsiPolling(m_rsiCandleSize, m_maxRsiToNotify, m_rsiMemorySize);
            IPollingResponse response = await priceAndRsiPolling.Start(currency, cancellationToken, currentTime);
            s_logger.LogInformation($"{currency}_{age}: Done phase 1: wait until lower price and higher RSI {response.Time}"); 
            return response.Time;
        }

        public bool ValidateCandleIsRed(string currency, 
            int age, 
            DateTime currentTime)
        {
            s_logger.LogInformation($"{currency}_{age} Start phase 2: validate current candle is red {currentTime}");
            RedCandleValidator redCandleValidator = m_cryptoBotPhasesFactory.CreateRedCandleValidator(m_redCandleSize);
            bool isCurrentCandleRed = redCandleValidator.Validate(currency, currentTime);
            s_logger.LogInformation($"{currency}_{age} Done phase 2: current candle is red: {isCurrentCandleRed} {currentTime}");
            return isCurrentCandleRed;        
        }

        public bool ValidateCandleIsGreen(string currency, 
            int age, 
            DateTime currentTime)
        {
            s_logger.LogInformation($"{currency}_{age} Start phase 3: validate current candle is green {currentTime}");
            GreenCandleValidator greenCandleValidator = m_cryptoBotPhasesFactory.CreateGreenCandleValidator(m_greenCandleSize);
            bool isCurrentCandleGreen = greenCandleValidator.Validate(currency, currentTime);
            s_logger.LogInformation($"{currency}_{age} Done phase 3: current candle is green: {isCurrentCandleGreen} {currentTime}");
            return isCurrentCandleGreen;
        }
        
        public async Task<(bool,DateTime)> WaitUnitPriceChange(decimal basePrice, 
            string currency,
            int age,
            CancellationToken cancellationToken,
            DateTime currentTime)
        {
            s_logger.LogInformation($"{currency}_{age} Start phase 4: get price every {m_priceChangeDelayTimeIterationsInSeconds / 60} minutes until it changed by {m_priceChangeToNotify}%, price: {basePrice}, {currentTime}");
            currentTime = await m_cryptoBotPhasesFactory.SystemClock.Wait(cancellationToken, currency,m_minutesToWaitBeforePollingPrice*60, "FullMode_WaitBeforeStartChild", currentTime);
            CandleCryptoPolling candlePolling = m_cryptoBotPhasesFactory.CreateCandlePolling(basePrice, m_priceChangeDelayTimeIterationsInSeconds, m_priceChangeCandleSize, m_priceChangeToNotify);
            IPollingResponse pollingResponse = await candlePolling.Start(currency,cancellationToken, currentTime);
            if (!(pollingResponse is CandlePollingResponse candlePollingResponse))
            {
                throw new Exception("candle polling response should be of type CandlePollingResponse");
            }

            string increaseOrDecreaseStr = candlePollingResponse.IsGain ? "increase by" : "decreased by";
            s_logger.LogInformation($"{currency}_{age} Done phase 4: price {increaseOrDecreaseStr} {m_priceChangeToNotify}%, {pollingResponse.Time}");
            return (candlePollingResponse.IsGain, pollingResponse.Time);
        }
    }
}