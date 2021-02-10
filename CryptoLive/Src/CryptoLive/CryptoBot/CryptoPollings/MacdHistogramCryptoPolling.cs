using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Abstractions;
using Common.PollingResponses;
using CryptoBot.Abstractions;
using Infra;
using Microsoft.Extensions.Logging;
using Storage.Abstractions;
using Utils.Abstractions;

namespace CryptoBot.CryptoPollings
{
    public class MacdHistogramCryptoPolling : ICryptoPolling
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<MacdHistogramCryptoPolling>();

        private readonly ICurrencyDataProvider m_currencyDataProvider;
        private readonly ISystemClock m_systemClock;
        private readonly INotificationService m_notificationService;
        private readonly int m_candleSizeInMinutes;
        private readonly int m_maxMacdPollingTime;

        public MacdHistogramCryptoPolling(INotificationService notificationService,
            ICurrencyDataProvider currencyDataProvider, 
            ISystemClock systemClock,
            int candleSizeInMinutes,
            int maxMacdPollingTime)
        {
            m_notificationService = notificationService;
            m_currencyDataProvider = currencyDataProvider;
            m_systemClock = systemClock;
            m_candleSizeInMinutes = candleSizeInMinutes;
            m_maxMacdPollingTime = maxMacdPollingTime;
        }

        public async Task<IPollingResponse> StartAsync(string symbol, CancellationToken cancellationToken, DateTime currentTime)
        {
            s_logger.LogDebug($"{symbol}: {nameof(MacdHistogramCryptoPolling)} start, " +
                              $"Get update every 1 minute," +
                              $"Max iteration {m_maxMacdPollingTime}");

            MacdHistogramPollingResponse macdHistogramPollingResponse;
            
            for(int i = 0; i < m_maxMacdPollingTime; i++)
            {
                decimal macdHistogram = m_currencyDataProvider.GetMacdHistogram(symbol, m_candleSizeInMinutes, currentTime);
                if (macdHistogram > 0)
                {
                    macdHistogramPollingResponse = new MacdHistogramPollingResponse(currentTime, macdHistogram);
                    string message =
                        $"{symbol}: {nameof(MacdHistogramCryptoPolling)} done, {macdHistogramPollingResponse}";
                    m_notificationService.Notify(message);
                    s_logger.LogDebug(message);
                    return macdHistogramPollingResponse;
                }
                currentTime = await m_systemClock.Wait(cancellationToken, symbol, 60, "MACD", currentTime);
            }
            
            macdHistogramPollingResponse = new MacdHistogramPollingResponse(currentTime, 0);
            s_logger.LogDebug($"{symbol}: {nameof(MacdHistogramCryptoPolling)} done, {macdHistogramPollingResponse}");
            return macdHistogramPollingResponse;
        }
    }
}