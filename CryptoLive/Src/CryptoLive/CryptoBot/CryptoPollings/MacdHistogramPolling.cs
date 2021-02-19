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
        private readonly int m_maxMacdPollingTimeInMinutes;
        private DateTime m_currentTime;

        public MacdHistogramCryptoPolling(INotificationService notificationService,
            ICurrencyDataProvider currencyDataProvider, 
            ISystemClock systemClock,
            int maxMacdPollingTimeInMinutes)
        {
            m_notificationService = notificationService;
            m_currencyDataProvider = currencyDataProvider;
            m_systemClock = systemClock;
            m_maxMacdPollingTimeInMinutes = maxMacdPollingTimeInMinutes;
        }

        public async Task<PollingResponseBase> StartAsync(string currency, CancellationToken cancellationToken, DateTime currentTime)
        {
            PollingResponseBase pollingResponse;
            m_currentTime = currentTime;
            s_logger.LogDebug(StartPollingDescription(currency));

            try
            {
                pollingResponse =  await StartAsyncImpl(currency, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                s_logger.LogWarning("got cancellation request");
                pollingResponse = CreateGotCancelledPollingResponse();
            }
            catch (Exception e)
            {
                s_logger.LogWarning(e, $"Failed, {e.Message}");
                pollingResponse = CreateExceptionPollingResponse(e);
            }
            s_logger.LogDebug(EndPollingDescription(currency, pollingResponse));
            return pollingResponse;
        }

        private static string EndPollingDescription(string currency, PollingResponseBase pollingResponse)
        {
            return $"{currency}: {nameof(MacdHistogramCryptoPolling)} done, {pollingResponse}";
        }

        private string StartPollingDescription(string currency)
        {
            return $"{currency}: {nameof(MacdHistogramCryptoPolling)} start, " +
                   $"Get update every 1 minute," +
                   $"Max iteration {m_maxMacdPollingTimeInMinutes}";
        }

        private async Task<MacdHistogramPollingResponse> StartAsyncImpl(string currency,
            CancellationToken cancellationToken)
        {
            decimal macdHistogram =
                m_currencyDataProvider.GetMacdHistogram(currency, m_currentTime);
            for (int i = 0; i < m_maxMacdPollingTimeInMinutes && macdHistogram < 0; i++)
            {
                m_currentTime = await m_systemClock.Wait(cancellationToken, currency, s_timeToWaitInSeconds,
                    s_actionName,
                    m_currentTime);
                macdHistogram =
                    m_currencyDataProvider.GetMacdHistogram(currency, m_currentTime);
            }

            if (macdHistogram >= 0)
            {
                MacdHistogramPollingResponse macdHistogramPollingResponse =
                        new MacdHistogramPollingResponse(m_currentTime, macdHistogram);
                    string message =
                        $"{currency}: {nameof(MacdHistogramCryptoPolling)} done, {macdHistogramPollingResponse}";
                    m_notificationService.Notify(message);
                    return macdHistogramPollingResponse;
            }

            return CreateReachedMaxTimeMacdHistogramPollingResponse();
        }

        private MacdHistogramPollingResponse CreateReachedMaxTimeMacdHistogramPollingResponse() => 
            new MacdHistogramPollingResponse(m_currentTime, 0, true);

        private MacdHistogramPollingResponse CreateGotCancelledPollingResponse() => 
            new MacdHistogramPollingResponse(m_currentTime, 0, false, true);

        private MacdHistogramPollingResponse CreateExceptionPollingResponse(Exception e) => 
            new MacdHistogramPollingResponse(m_currentTime, 0, false, false, e);
    }
}