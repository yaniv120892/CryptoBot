using System;
using System.Threading;
using System.Threading.Tasks;
using Common.PollingResponses;
using Infra;
using Microsoft.Extensions.Logging;
using Utils.Abstractions;

namespace Utils.CryptoPollings
{
    public class RsiPolling : IPolling
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<RsiPolling>();
        private static readonly int s_timeToWaitInSeconds = 60;

        private readonly ICurrencyService m_currencyService;
        private readonly ISystemClock m_systemClock;
        private readonly INotificationHandler m_notificationHandler;
        private readonly int m_candleSizeInMinutes;
        private readonly int m_candlesAmount;

        public RsiPolling(INotificationHandler notificationHandler,
            ICurrencyService currencyService, 
            ISystemClock systemClock,
            int candleSizeInMinutes,
            int candlesAmount)
        {
            m_notificationHandler = notificationHandler;
            m_currencyService = currencyService;
            m_systemClock = systemClock;
            m_candleSizeInMinutes = candleSizeInMinutes;
            m_candlesAmount = candlesAmount;
        }

        public async Task<IPollingResponse> StartPolling(string desiredSymbol, CancellationToken cancellationToken, DateTime currentTime)
        {
            s_logger.LogInformation($"{desiredSymbol}: Start crypto rsi polling, Get update every {s_timeToWaitInSeconds / 60} minutes");
            decimal rsi = await m_currencyService.GetRsiAsync(desiredSymbol, m_candleSizeInMinutes, currentTime, m_candlesAmount);
            while (!m_notificationHandler.NotifyIfNeeded(rsi, desiredSymbol))
            {
                currentTime = await m_systemClock.Wait(cancellationToken, desiredSymbol, s_timeToWaitInSeconds, "RSI", currentTime);
                rsi = await m_currencyService.GetRsiAsync(desiredSymbol, m_candleSizeInMinutes, currentTime, m_candlesAmount);
            }

            s_logger.LogInformation($"{desiredSymbol}: Done crypto rsi polling");
            return new RsiPollingResponse(currentTime, rsi);
        }
    }
}