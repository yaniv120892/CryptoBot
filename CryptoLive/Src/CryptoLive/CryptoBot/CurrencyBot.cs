using System;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Abstractions;
using Common.PollingResponses;
using CryptoBot.Abstractions;
using CryptoBot.CryptoPollings;
using CryptoBot.CryptoValidators;
using Infra;
using Microsoft.Extensions.Logging;
using Utils.Converters;

namespace CryptoBot
{
    public class CurrencyBot
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<CurrencyBot>();
        
        private readonly ICryptoBotPhasesFactory m_cryptoBotPhasesFactory;
        private readonly decimal m_maxRsiToNotify;
        private readonly int m_rsiCandleSize;
        private readonly int m_redCandleSize;
        private readonly int m_macdCandleSize;
        private readonly int m_greenCandleSize;
        private readonly int m_priceChangeDelayTimeIterationsInSeconds;
        private readonly decimal m_priceChangeToNotify;
        private readonly int m_priceChangeCandleSize;
        private readonly int m_rsiMemorySize;
        private readonly int m_minutesToWaitBeforePollingPrice;
        private readonly int m_maxMacdPollingTime;

        public string Currency { get; }

        public CurrencyBot(ICryptoBotPhasesFactory cryptoBotPhasesFactory,
            string currency, 
            decimal maxRsiToNotify, 
            int rsiCandleSize, 
            int redCandleSize, 
            int greenCandleSize, 
            int priceChangeDelayTimeIterationsInSeconds, 
            decimal priceChangeToNotify, 
            int priceChangeCandleSize,
            int rsiMemorySize, 
            int macdCandleSize,
            int minutesToWaitBeforePollingPrice, 
            int maxMacdPollingTime)
        {
            m_cryptoBotPhasesFactory = cryptoBotPhasesFactory;
            Currency = currency;
            m_maxRsiToNotify = maxRsiToNotify;
            m_rsiCandleSize = rsiCandleSize;
            m_redCandleSize = redCandleSize;
            m_greenCandleSize = greenCandleSize;
            m_priceChangeDelayTimeIterationsInSeconds = priceChangeDelayTimeIterationsInSeconds;
            m_priceChangeToNotify = priceChangeToNotify;
            m_priceChangeCandleSize = priceChangeCandleSize;
            m_rsiMemorySize = rsiMemorySize;
            m_macdCandleSize = macdCandleSize;
            m_minutesToWaitBeforePollingPrice = minutesToWaitBeforePollingPrice;
            m_maxMacdPollingTime = maxMacdPollingTime;
        }
        
        public async Task<(BotResult, DateTime)> StartAsync(DateTime currentTime, int botVersion)
        {
            int res;
            (res, currentTime) = await RunGen(currentTime, botVersion, 0, new CancellationTokenSource());
            BotResult botResult = BotResultConverter.ConvertIntToBotResult(res);
            return (botResult, currentTime);
        }

        private async Task<(int, DateTime)> RunGen(DateTime currentTime, int botVersion, int age, CancellationTokenSource cancellationTokenSource)
        {
            return botVersion switch
            {
                1 => await RunFullModeVersion1Async(Currency, cancellationTokenSource, age, currentTime),
                2 => await RunFullModeVersion2Async(Currency, cancellationTokenSource, age, currentTime),
                3 => await RunFullModeVersion3Async(Currency, cancellationTokenSource, age, currentTime),
                _ => throw new ArgumentException("Unknown bot version")
            };
        }

        private async Task<(int res, DateTime currentTime)> RunFullModeVersion3Async(string currency, CancellationTokenSource cancellationTokenSource, int age, DateTime currentTime)
        {
            const int botVersion = 3;
            s_logger.LogInformation($"{currency}_{age}: Start iteration");
            
            // Indicator
            currentTime = await WaitUntilRsiIsBelowMaxValue(currency, CancellationToken.None, age, currentTime);
            
            // Validator
            bool isMacdNegative = ValidateMacdNegative(currency, age, currentTime);
            if(!isMacdNegative)
            {
                s_logger.LogInformation($"{currency}_{age}: Done iteration - MACD is not negative");
                currentTime = await m_cryptoBotPhasesFactory.SystemClock.Wait(cancellationTokenSource.Token,currency,1*60, "FullMode wait after MACD not negative", currentTime);
                return (0,currentTime);
            }
            Task<(int, DateTime)> child = RunFullModeChildAsync(currency, cancellationTokenSource, age, currentTime, botVersion);
            currentTime = await m_cryptoBotPhasesFactory.SystemClock.Wait(cancellationTokenSource.Token,currency,1*60, "FullMode_WaitAfterCandleIsRed", currentTime);

            bool isMacdPositive;
            (isMacdPositive, currentTime) = await WaitUntilMacdIsPositive(currency, currentTime, age, cancellationTokenSource.Token);

            if(!isMacdPositive)
            {
                s_logger.LogInformation($"{currency}_{age}: Done iteration - macd is not positive after 2h");
                return await child;
            }

            cancellationTokenSource.Cancel();
            
            // Enter
            decimal basePrice = await m_cryptoBotPhasesFactory.CurrencyDataProvider.GetPriceAsync(currency, currentTime);
            bool isGain;
            (isGain, currentTime) = await WaitUnitPriceChange(basePrice, currency, age, currentTime);
            if (isGain)
            {
                s_logger.LogInformation($"{currency}_{age}: Done iteration - Gain {currentTime}");
                return (1, currentTime);
            }

            s_logger.LogInformation($"{currency}_{age}: Done iteration - Loss {currentTime}");
            return (-1, currentTime);
        }

        private async Task<(bool isMacdPositive, DateTime currentTime)> WaitUntilMacdIsPositive(string currency, DateTime currentTime, int age, CancellationToken cancellationToken)
        {
            s_logger.LogInformation($"{currency}_{age} Start phase 3: wait until MACD is positive {currentTime}");
            MacdHistogramCryptoPolling macdHistogramPolling = m_cryptoBotPhasesFactory.CreateMacdPolling(m_macdCandleSize, m_maxMacdPollingTime);
            MacdHistogramPollingResponse macdHistogramPollingResponse = (MacdHistogramPollingResponse) await macdHistogramPolling.Start(currency, cancellationToken ,currentTime);
            s_logger.LogInformation($"{currency}_{age} Done phase 3: wait until MACD is positive {macdHistogramPollingResponse.Time} :");
            bool isPositiveMacdHistogram = macdHistogramPollingResponse.MacdHistogram > 0;
            return (isPositiveMacdHistogram, macdHistogramPollingResponse.Time);
        }

        private bool ValidateMacdNegative(string currency, int age, DateTime currentTime)
        {
            s_logger.LogInformation($"{currency}_{age} Start phase 2: validate current macd is negative {currentTime}");
            MacdHistogramNegativeValidator macdHistogramNegativeValidator = m_cryptoBotPhasesFactory.CreateMacdNegativeValidator(m_macdCandleSize);
            bool isCurrentMacdNegative = macdHistogramNegativeValidator.Validate(currency, currentTime);
            s_logger.LogInformation($"{currency}_{age} Done phase 2: validate current macd is negative {isCurrentMacdNegative} {currentTime}");
            return isCurrentMacdNegative;            
        }

        private async Task<(int res, DateTime currentTime)> RunFullModeVersion2Async(string currency, CancellationTokenSource cancellationTokenSource, int age, DateTime currentTime)
        {
            const int botVersion = 2;
            s_logger.LogInformation($"{currency}_{age}: Start iteration");
            
            // Indicator
            currentTime = await WaitUntilLowerPriceAndHigherRsi(currency, currentTime, age);
            
            // Validator
            bool isCandleRed = ValidateCandleIsRed(currency, age, currentTime);
            if(!isCandleRed)
            {
                s_logger.LogInformation($"{currency}_{age}: Done iteration - candle is not red");
                currentTime = await m_cryptoBotPhasesFactory.SystemClock.Wait(cancellationTokenSource.Token,currency,1*60, "FullMode_WaitAfterCandleNotRed", currentTime);
                return (0,currentTime);
            }
            Task<(int, DateTime)> child = RunFullModeChildAsync(currency, cancellationTokenSource, age, currentTime, botVersion);
            currentTime = await m_cryptoBotPhasesFactory.SystemClock.Wait(cancellationTokenSource.Token,currency,15*60, "FullMode_WaitAfterCandleIsRed", currentTime);
            
            bool isCandleGreen = ValidateCandleIsGreen(currency, age, currentTime);
            if(!isCandleGreen)
            {
                s_logger.LogInformation($"{currency}_{age}: Done iteration - candle is not green");
                return await child;
            }

            cancellationTokenSource.Cancel();
            
            // Enter
            decimal basePrice = await m_cryptoBotPhasesFactory.CurrencyDataProvider.GetPriceAsync(currency, currentTime);
            bool isGain;
            (isGain, currentTime) = await WaitUnitPriceChange(basePrice, currency, age, currentTime);
            if (isGain)
            {
                s_logger.LogInformation($"{currency}_{age}: Done iteration - Gain {currentTime}");
                return (1, currentTime);
            }

            s_logger.LogInformation($"{currency}_{age}: Done iteration - Loss {currentTime}");
            return (-1, currentTime);
        }
        

        private async Task<(int, DateTime)> RunFullModeVersion1Async(string currency, CancellationTokenSource cancellationTokenSource, int age, DateTime currentTime)
        {
            const int botVersion = 1;
            s_logger.LogInformation($"{currency}_{age}: Start iteration");
            
            // Indicator
            currentTime = await WaitUntilRsiIsBelowMaxValue(currency, cancellationTokenSource.Token, age, currentTime);
            
            // Validator
            bool isCandleRed = ValidateCandleIsRed(currency, age, currentTime);
            if(!isCandleRed)
            {
                s_logger.LogInformation($"{currency}_{age}: Done iteration - candle is not red");
                currentTime = await m_cryptoBotPhasesFactory.SystemClock.Wait(cancellationTokenSource.Token,currency,1*60, "FullMode_WaitAfterCandleNotRed", currentTime);
                return (0,currentTime);
            }

            Task<(int, DateTime)> child = RunFullModeChildAsync(currency, cancellationTokenSource, age, currentTime, botVersion);
            currentTime = await m_cryptoBotPhasesFactory.SystemClock.Wait(cancellationTokenSource.Token,currency,15*60, "FullMode_WaitAfterCandleIsRed", currentTime);
            
            bool isCandleGreen = ValidateCandleIsGreen(currency, age, currentTime);
            if(!isCandleGreen)
            {
                s_logger.LogInformation($"{currency}_{age}: Done iteration - candle is not green");
                return await child;
            }

            cancellationTokenSource.Cancel();
            
            // Enter
            decimal basePrice = await m_cryptoBotPhasesFactory.CurrencyDataProvider.GetPriceAsync(currency, currentTime);
            bool isGain;
            (isGain, currentTime) = await WaitUnitPriceChange(basePrice, currency, age, currentTime);
            if (isGain)
            {
                s_logger.LogInformation($"{currency}_{age}: Done iteration - Gain {currentTime}");
                return (1, currentTime);
            }

            s_logger.LogInformation($"{currency}_{age}: Done iteration - Loss {currentTime}");
            return (-1, currentTime);
        }

        private async Task<(int, DateTime)> RunFullModeChildAsync(string currency,
            CancellationTokenSource cancellationTokenSource, int age, DateTime currentTime, int botVersion)
        {
            s_logger.LogDebug($"{currency}_{age}: Start child {currentTime}");
            age += 1;
            currentTime = await m_cryptoBotPhasesFactory.SystemClock.Wait(cancellationTokenSource.Token,currency,1*60, "FullMode_WaitBeforeStartChild", currentTime);
            return await RunGen(currentTime, botVersion, age, cancellationTokenSource);
        }

        private async Task<DateTime> WaitUntilRsiIsBelowMaxValue(string currency, CancellationToken cancellationToken, int age, DateTime currentTime)
        {
            s_logger.LogInformation($"{currency}_{age}: Start phase 1: wait until RSI is below {m_maxRsiToNotify} {currentTime}");
            RsiCryptoPolling rsiPolling = m_cryptoBotPhasesFactory.CreateRsiPolling(m_maxRsiToNotify);
            IPollingResponse response = await rsiPolling.Start(currency, cancellationToken, currentTime);
            s_logger.LogInformation($"{currency}_{age}: Done phase 1: RSI is below {m_maxRsiToNotify} {response.Time}");
            return response.Time;
        }
        
        private async Task<DateTime> WaitUntilLowerPriceAndHigherRsi(string currency, DateTime currentTime, int age)
        {
            s_logger.LogInformation($"{currency}_{age}: Start phase 1: wait until lower price and higher RSI is {currentTime}");
            PriceAndRsiCryptoPolling priceAndRsiPolling = m_cryptoBotPhasesFactory.CreatePriceAndRsiPolling(m_rsiCandleSize, m_maxRsiToNotify, m_rsiMemorySize);
            IPollingResponse response = await priceAndRsiPolling.Start(currency, CancellationToken.None, currentTime);
            s_logger.LogInformation($"{currency}_{age}: Done phase 1: wait until lower price and higher RSI {response.Time}"); 
            return response.Time;
        }

        private bool ValidateCandleIsRed(string currency, int age, DateTime currentTime)
        {
            s_logger.LogInformation($"{currency}_{age} Start phase 2: validate current candle is red {currentTime}");
            RedCandleValidator redCandleValidator = m_cryptoBotPhasesFactory.CreateRedCandleValidator(m_redCandleSize);
            bool isCurrentCandleRed = redCandleValidator.Validate(currency, currentTime);
            s_logger.LogInformation($"{currency}_{age} Done phase 2: current candle is red: {isCurrentCandleRed} {currentTime}");
            return isCurrentCandleRed;        
        }

        private bool ValidateCandleIsGreen(string currency, int age, DateTime currentTime)
        {
            s_logger.LogInformation($"{currency}_{age} Start phase 3: validate current candle is green {currentTime}");
            GreenCandleValidator greenCandleValidator = m_cryptoBotPhasesFactory.CreateGreenCandleValidator(m_greenCandleSize);
            bool isCurrentCandleGreen = greenCandleValidator.Validate(currency, currentTime);
            s_logger.LogInformation($"{currency}_{age} Done phase 3: current candle is green: {isCurrentCandleGreen} {currentTime}");
            return isCurrentCandleGreen;
        }
        
        private async Task<(bool,DateTime)> WaitUnitPriceChange(decimal basePrice, string currency, int age, DateTime currentTime)
        {
            s_logger.LogInformation($"{currency}_{age} Start phase 4: get price every {m_priceChangeDelayTimeIterationsInSeconds / 60} minutes until it changed by {m_priceChangeToNotify}%, price: {basePrice}, {currentTime}");
            currentTime = await m_cryptoBotPhasesFactory.SystemClock.Wait(CancellationToken.None, currency,m_minutesToWaitBeforePollingPrice*60, "FullMode_WaitBeforeStartChild", currentTime);
            CandleCryptoPolling candlePolling = m_cryptoBotPhasesFactory.CreateCandlePolling(basePrice, m_priceChangeDelayTimeIterationsInSeconds, m_priceChangeCandleSize, m_priceChangeToNotify);
            IPollingResponse pollingResponse = await candlePolling.Start(currency,CancellationToken.None, currentTime);
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