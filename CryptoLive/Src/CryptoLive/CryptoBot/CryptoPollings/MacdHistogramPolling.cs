using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Abstractions;
using Common.PollingResponses;
using CryptoBot.Abstractions;
using Infra;
using Microsoft.Extensions.Logging;
using Storage.Abstractions.Providers;
using Utils.Abstractions;

namespace CryptoBot.CryptoPollings
{
    public class MacdHistogramCryptoPolling : ICryptoPolling
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<MacdHistogramCryptoPolling>();
        private static readonly int s_timeToWaitInSeconds = 60;
        private static readonly string s_actionName = "Macd polling";
        
        private readonly ICurrencyDataProvider m_currencyDataProvider;
        private readonly ISystemClock m_systemClock;
        private readonly INotificationService m_notificationService;
        private readonly int m_candleSizeInMinutes;
        private readonly int m_maxMacdPollingTimeInMinutes;

        public MacdHistogramCryptoPolling(INotificationService notificationService,
            ICurrencyDataProvider currencyDataProvider, 
            ISystemClock systemClock,
            int candleSizeInMinutes,
            int maxMacdPollingTimeInMinutes)
        {
            m_notificationService = notificationService;
            m_currencyDataProvider = currencyDataProvider;
            m_systemClock = systemClock;
            m_candleSizeInMinutes = candleSizeInMinutes;
            m_maxMacdPollingTimeInMinutes = maxMacdPollingTimeInMinutes;
        }

        public async Task<PollingResponseBase> StartAsync(string currency, CancellationToken cancellationToken, DateTime currentTime)
        {
            s_logger.LogDebug($"{currency}: {nameof(MacdHistogramCryptoPolling)} start, " +
                              $"Get update every 1 minute," +
                              $"Max iteration {m_maxMacdPollingTimeInMinutes}");

            MacdHistogramPollingResponse macdHistogramPollingResponse =  await StartAsyncImpl(currency, cancellationToken, currentTime);
            s_logger.LogDebug($"{currency}: {nameof(MacdHistogramCryptoPolling)} done, {macdHistogramPollingResponse}");
            return macdHistogramPollingResponse;
        }

        private async Task<MacdHistogramPollingResponse> StartAsyncImpl(string currency, CancellationToken cancellationToken, DateTime currentTime)
        {
            try
            {
                for (int i = 0; i < m_maxMacdPollingTimeInMinutes; i++)
                {
                    currentTime = await m_systemClock.Wait(cancellationToken, currency, s_timeToWaitInSeconds, s_actionName,
                        currentTime);
                    decimal macdHistogram = m_currencyDataProvider.GetMacdHistogram(currency, m_candleSizeInMinutes, currentTime);
                    if (macdHistogram > 0)
                    {
                        MacdHistogramPollingResponse macdHistogramPollingResponse = new MacdHistogramPollingResponse(currentTime, macdHistogram);
                        string message =
                            $"{currency}: {nameof(MacdHistogramCryptoPolling)} done, {macdHistogramPollingResponse}";
                        m_notificationService.Notify(message);
                        return macdHistogramPollingResponse;
                    }
                }

                return new MacdHistogramPollingResponse(currentTime, 0, true);
            }
            catch (OperationCanceledException)
            {
                s_logger.LogWarning("got cancellation request");
                return new MacdHistogramPollingResponse(currentTime, 0, false, true);
            }
            catch (Exception e)
            {
                s_logger.LogWarning(e, $"Failed, {e.Message}");
                return new MacdHistogramPollingResponse(currentTime, 0, false, false, e);
            }
        }
    }
}