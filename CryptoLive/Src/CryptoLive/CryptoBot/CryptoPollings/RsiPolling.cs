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
    public class RsiCryptoPolling : ICryptoPolling
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<RsiCryptoPolling>();
        private static readonly int s_timeToWaitInSeconds = 60;

        private readonly ICurrencyDataProvider m_currencyDataProvider;
        private readonly ISystemClock m_systemClock;
        private readonly INotificationService m_notificationService;
        private readonly decimal m_maxRsiToNotify;
        private DateTime m_currentTime;
        private static string s_actionName = "RSI";

        public RsiCryptoPolling(INotificationService notificationService,
            ICurrencyDataProvider currencyDataProvider, 
            ISystemClock systemClock,
            decimal maxRsiToNotify)
        {
            m_notificationService = notificationService;
            m_currencyDataProvider = currencyDataProvider;
            m_systemClock = systemClock;
            m_maxRsiToNotify = maxRsiToNotify;
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

        private async Task<PollingResponseBase> StartAsyncImpl(string currency, CancellationToken cancellationToken)
        {
            decimal rsi = m_currencyDataProvider.GetRsi(currency, m_currentTime);
            while (rsi >= m_maxRsiToNotify)
            {
                m_currentTime = await m_systemClock.Wait(cancellationToken, currency, s_timeToWaitInSeconds,
                    s_actionName, m_currentTime);
                rsi = m_currencyDataProvider.GetRsi(currency, m_currentTime);
            }

            var rsiPollingResponse = new RsiPollingResponse(m_currentTime, rsi);
            string notificationMessage = $"{currency}: {nameof(RsiCryptoPolling)} done, {rsiPollingResponse}";
            m_notificationService.Notify(notificationMessage);
            return rsiPollingResponse;
        }

        private static string EndPollingDescription(string currency, PollingResponseBase pollingResponse) =>
            $"{currency}: {nameof(RsiCryptoPolling)} done, {pollingResponse}";

        private PollingResponseBase CreateExceptionPollingResponse(Exception exception) => 
            new RsiPollingResponse(m_currentTime, -1, false, exception);

        private PollingResponseBase CreateGotCancelledPollingResponse() => 
            new RsiPollingResponse(m_currentTime, -1, true);

        private static string StartPollingDescription(string currency) => 
            $"{currency}: {nameof(RsiCryptoPolling)}, Get update every {s_timeToWaitInSeconds / 60} minutes";
    }
}