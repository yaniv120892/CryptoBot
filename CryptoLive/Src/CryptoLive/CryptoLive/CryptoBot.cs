using System;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.PollingResponses;
using Infra;
using Microsoft.Extensions.Logging;
using Utils.Abstractions;
using Utils.Converters;
using Utils.CryptoPollings;
using Utils.CryptoValidators;

namespace CryptoLive
{
    public class CryptoBot
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<CryptoBot>();
        private readonly ICryptoBotPhasesFactory m_cryptoBotPhasesFactory;
        private readonly decimal m_maxRsiToNotify;
        private readonly int m_rsiCandleSize;
        private readonly int m_redCandleSize;
        private readonly int m_greenCandleSize;
        private readonly int m_priceChangeDelayTimeIterationsInSeconds;
        private readonly decimal m_priceChangeToNotify;
        private readonly int m_priceChangeCandleSize;
        private readonly int m_rsiCandlesAmount;

        public string Currency { get; }

        public CryptoBot(ICryptoBotPhasesFactory cryptoBotPhasesFactory,
            string currency, 
            decimal maxRsiToNotify, 
            int rsiCandleSize, 
            int redCandleSize, 
            int greenCandleSize, 
            int priceChangeDelayTimeIterationsInSeconds, 
            decimal priceChangeToNotify, 
            int priceChangeCandleSize,
            int rsiCandlesAmount)
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
            m_rsiCandlesAmount = rsiCandlesAmount;
        }
        
        public async Task<(BotResult, DateTime)> StartAsync(DateTime currentTime)
        {
            int res;
            (res, currentTime) = await RunFullModeAsync(Currency, new CancellationTokenSource(), 0, currentTime);
            BotResult botResult = BotResultConverter.ConvertIntToBotResult(res);
            return (botResult, currentTime);
        }

        private async Task<(int, DateTime)> RunFullModeAsync(string currency, CancellationTokenSource cancellationTokenSource, int age, DateTime currentTime)
        {
            s_logger.LogInformation($"{currency}_{age}: Start iteration");
            currentTime = await WaitUntilRsiIsBelowMaxValue(currency, cancellationTokenSource.Token, age, currentTime);
            
            bool isCandleRed = await ValidateCandleIsRed(currency, age, currentTime);
            if(!isCandleRed)
            {
                s_logger.LogInformation($"{currency}_{age}: Done iteration - candle is not red");
                currentTime = await m_cryptoBotPhasesFactory.SystemClock.Wait(cancellationTokenSource.Token,currency,1*60, "FullMode_WaitAfterCandleNotRed", currentTime);
                return (0,currentTime);
            }

            Task<(int, DateTime)> child = RunFullModeChildAsync(currency, cancellationTokenSource, age, currentTime);
            currentTime = await m_cryptoBotPhasesFactory.SystemClock.Wait(cancellationTokenSource.Token,currency,15*60, "FullMode_WaitAfterCandleIsRed", currentTime);
            
            bool isCandleGreen = await ValidateCandleIsGreen(currency, age, currentTime);
            if(!isCandleGreen)
            {
                s_logger.LogInformation($"{currency}_{age}: Done iteration - candle is not green");
                return await child;
            }

            cancellationTokenSource.Cancel();
            decimal basePrice = await m_cryptoBotPhasesFactory.CurrencyService.GetPriceAsync(currency, currentTime);
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
            CancellationTokenSource cancellationTokenSource, int age, DateTime currentTime)
        {
            s_logger.LogDebug($"{currency}_{age}: Start child {currentTime}");
            age += 1;
            currentTime = await m_cryptoBotPhasesFactory.SystemClock.Wait(cancellationTokenSource.Token,currency,1*60, "FullMode_WaitBeforeStartChild", currentTime);
            return await RunFullModeAsync(currency, cancellationTokenSource, age, currentTime);
        }

        private async Task<DateTime> WaitUntilRsiIsBelowMaxValue(string currency, CancellationToken cancellationToken, int age, DateTime currentTime)
        {
            s_logger.LogInformation($"{currency}_{age}: Start phase 1: wait until RSI is below {m_maxRsiToNotify} {currentTime}");
            RsiPolling rsiPolling = m_cryptoBotPhasesFactory.CreateRsiPolling(m_rsiCandleSize, m_maxRsiToNotify, m_rsiCandlesAmount);
            IPollingResponse response = await rsiPolling.StartPolling(currency, cancellationToken, currentTime);
            s_logger.LogInformation($"{currency}_{age}: Done phase 1: RSI is below {m_maxRsiToNotify} {response.Time}");
            return response.Time;
        }

        private async Task<bool> ValidateCandleIsRed(string currency, int age, DateTime currentTime)
        {
            s_logger.LogInformation($"{currency}_{age} Start phase 2: validate current candle is red {currentTime}");
            RedCandleValidator redCandleValidator = m_cryptoBotPhasesFactory.CreateRedCandleValidator(m_redCandleSize);
            bool isCurrentCandleRed = await redCandleValidator.Validate(currency, currentTime);
            s_logger.LogInformation($"{currency}_{age} Done phase 2: current candle is red: {isCurrentCandleRed} {currentTime}");
            return isCurrentCandleRed;        
        }

        private async Task<bool> ValidateCandleIsGreen(string currency, int age, DateTime currentTime)
        {
            s_logger.LogInformation($"{currency}_{age} Start phase 3: validate current candle is green {currentTime}");
            GreenCandleValidator greenCandleValidator = m_cryptoBotPhasesFactory.CreateGreenCandleValidator(m_greenCandleSize);
            bool isCurrentCandleGreen = await greenCandleValidator.Validate(currency, currentTime);
            s_logger.LogInformation($"{currency}_{age} Done phase 3: current candle is green: {isCurrentCandleGreen} {currentTime}");
            return isCurrentCandleGreen;
        }
        
        private async Task<(bool,DateTime)> WaitUnitPriceChange(decimal basePrice, string currency, int age, DateTime currentTime)
        {
            s_logger.LogInformation($"{currency}_{age} Start phase 4: get price every {m_priceChangeDelayTimeIterationsInSeconds} seconds until it changed by {m_priceChangeToNotify}%");
            CandlePolling candlePolling = m_cryptoBotPhasesFactory.CreateCandlePolling(basePrice, m_priceChangeDelayTimeIterationsInSeconds, m_priceChangeCandleSize, m_priceChangeToNotify);
            IPollingResponse pollingResponse = await candlePolling.StartPolling(currency,CancellationToken.None, currentTime);
            if (!(pollingResponse is CandlePollingResponse candlePollingResponse))
            {
                throw new Exception("candle polling response should be of type CandlePollingResponse");
            }

            string increaseOrDecreaseStr = candlePollingResponse.IsGain ? "increase by" : "decreased by";
            s_logger.LogInformation($"{currency}_{age} Done phase 4: price {increaseOrDecreaseStr} {m_priceChangeToNotify}%");
            return (candlePollingResponse.IsGain, currentTime);
        }
    }
}