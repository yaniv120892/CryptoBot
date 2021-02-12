using System;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Abstractions;
using Common.PollingResponses;
using CryptoBot.Abstractions;
using Infra;
using Microsoft.Extensions.Logging;
using Storage.Abstractions;
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

        public async Task<IPollingResponse> StartAsync(string currency, CancellationToken cancellationToken, DateTime currentTime)
        {
            s_logger.LogDebug($"{currency}: {nameof(CandleCryptoPolling)}, " +
                              $"Get update every {m_delayTimeInSeconds / 60} minutes");
            MyCandle currCandle = m_currencyDataProvider.GetLastCandle(currency, m_candleSize, currentTime);
            (bool isBelow, bool isAbove) = IsCandleInRange(currCandle);
            while (isBelow == false && isAbove == false)
            {
                currentTime = await m_systemClock.Wait(cancellationToken, currency, m_delayTimeInSeconds, "Price range", currentTime);
                currCandle = m_currencyDataProvider.GetLastCandle(currency, 1, currentTime);
                (isBelow, isAbove) = IsCandleInRange(currCandle);
            }

            var candlePollingResponse = new CandlePollingResponse(isBelow, isAbove, currentTime, currCandle);
            string message = $"{currency}: {nameof(CandleCryptoPolling)} done, {candlePollingResponse}";
            m_notificationService.Notify(message);
            s_logger.LogDebug(message);
            return candlePollingResponse;
        }

        private (bool isBelow, bool isAbove) IsCandleInRange(MyCandle currCandle)
        {
            if (currCandle.Low < m_minPrice)
            {
                return (true, false);
            }
            
            return currCandle.High > m_maxPrice ? 
                (false, true) : 
                (false, false);
        }
    }
}