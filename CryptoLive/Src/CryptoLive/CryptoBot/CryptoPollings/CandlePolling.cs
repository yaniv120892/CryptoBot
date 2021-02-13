using System;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Abstractions;
using Common.PollingResponses;
using CryptoBot.Abstractions;
using Infra;
using Microsoft.Extensions.Logging;
using Storage.Abstractions.Providers;
using Utils.Abstractions;

namespace CryptoBot.CryptoPollings
{
    public class CandleCryptoPolling : ICryptoPolling
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<CandleCryptoPolling>();
        
        private readonly int m_delayTimeInSeconds;
        private readonly INotificationService m_notificationService;
        private readonly ICurrencyDataProvider m_currencyDataProvider;
        private readonly ISystemClock m_systemClock;
        private readonly int m_candleSize;
        private readonly decimal m_minPrice;
        private readonly decimal m_maxPrice;
        
        private MyCandle m_currCandle;
        private DateTime m_currentTime;

        public CandleCryptoPolling(INotificationService notificationService, 
            ICurrencyDataProvider currencyDataProvider, 
            ISystemClock systemClock,
            int delayTimeInSeconds, 
            int candleSize,
            decimal minPrice,
            decimal maxPrice)
        {
            if (delayTimeInSeconds / 60 != candleSize)
            {
                s_logger.LogError($"delayTimeInSeconds/60=candleSize should be true but delayTimeInSeconds={delayTimeInSeconds},candleSize={candleSize}");
                throw new ArgumentException(
                    $"delayTimeInSeconds/60=candleSize should be true but delayTimeInSeconds={delayTimeInSeconds},candleSize={candleSize}");
            }
            m_notificationService = notificationService;
            m_currencyDataProvider = currencyDataProvider;
            m_systemClock = systemClock;
            m_delayTimeInSeconds = delayTimeInSeconds;
            m_candleSize = candleSize;
            m_minPrice = minPrice;
            m_maxPrice = maxPrice;
        }

        public async Task<PollingResponseBase> StartAsync(string currency, CancellationToken cancellationToken, DateTime currentTime)
        {
            CandlePollingResponse candlePollingResponse;
            m_currentTime = currentTime;
            s_logger.LogDebug($"{currency}: {nameof(CandleCryptoPolling)}, " +
                              $"Get update every {m_delayTimeInSeconds / 60} minutes");
            try
            {
                candlePollingResponse =
                    await StartAsyncImpl(currency, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                s_logger.LogWarning("got cancellation request");
                candlePollingResponse = CreateGotCancelledPollingResponse();
            }
            catch (Exception e)
            {
                s_logger.LogWarning(e, $"Failed, {e.Message}");
                candlePollingResponse = CreateExceptionPollingResponse(e);
            }
            string message = $"{currency}: {nameof(CandleCryptoPolling)} done, {candlePollingResponse}";
            m_notificationService.Notify(message);
            s_logger.LogDebug(message);
            return candlePollingResponse;
        }

        private async Task<CandlePollingResponse> StartAsyncImpl(string currency,
            CancellationToken cancellationToken)
        {
            m_currCandle = m_currencyDataProvider.GetLastCandle(currency, m_candleSize, m_currentTime);
            (bool isBelow, bool isAbove) = IsCandleInRange(m_currCandle);
            while (isBelow == false && isAbove == false)
            {
                m_currentTime = await m_systemClock.Wait(cancellationToken, currency, m_delayTimeInSeconds,
                    "Price range", m_currentTime);
                m_currCandle = m_currencyDataProvider.GetLastCandle(currency, m_candleSize, m_currentTime);
                (isBelow, isAbove) = IsCandleInRange(m_currCandle);
            }

            return new CandlePollingResponse(isBelow, isAbove, m_currentTime, m_currCandle);
        }

        private (bool isBelow, bool isAbove) IsCandleInRange(MyCandle currCandle) =>
            (currCandle.Low < m_minPrice, currCandle.High > m_maxPrice);
        
        private CandlePollingResponse CreateExceptionPollingResponse(Exception e) => 
            new CandlePollingResponse(false, false, m_currentTime, m_currCandle, false, e);

        private CandlePollingResponse CreateGotCancelledPollingResponse() => 
            new CandlePollingResponse(false, false, m_currentTime, m_currCandle, true);
    }
}