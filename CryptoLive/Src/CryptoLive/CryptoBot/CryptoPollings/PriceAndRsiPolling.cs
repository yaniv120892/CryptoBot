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
    public class PriceAndRsiCryptoPolling : ICryptoPolling
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<PriceAndRsiCryptoPolling>();
        private static readonly int s_timeToWaitInSeconds = 60;

        private readonly INotificationService m_notificationService;
        private readonly ICurrencyDataProvider m_currencyDataProvider;
        private readonly ISystemClock m_systemClock;
        private readonly ICryptoPriceAndRsiQueue<PriceAndRsi> m_cryptoPriceAndRsiQueue;
        private readonly int m_candleSizeInMinutes;
        private readonly decimal m_maxRsiToNotify;
        private DateTime m_currentTime;

        public PriceAndRsiCryptoPolling(INotificationService notificationService,
            ICurrencyDataProvider currencyDataProvider,
            ISystemClock systemClock,
            ICryptoPriceAndRsiQueue<PriceAndRsi> cryptoPriceAndRsiQueue,
            int candleSizeInMinutes,
            decimal maxRsiToNotify)
        {
            m_notificationService = notificationService;
            m_currencyDataProvider = currencyDataProvider;
            m_systemClock = systemClock;
            m_candleSizeInMinutes = candleSizeInMinutes;
            m_maxRsiToNotify = maxRsiToNotify;
            m_cryptoPriceAndRsiQueue = cryptoPriceAndRsiQueue;
        }

        public async Task<PollingResponseBase> StartAsync(string currency, CancellationToken cancellationToken,
            DateTime currentTime)
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
            PriceAndRsi currentPriceAndRsi =
                m_currencyDataProvider.GetRsiAndClosePrice(currency, m_candleSizeInMinutes, m_currentTime);
            PriceAndRsi oldPriceAndRsi;
            while (ShouldContinuePolling(currentPriceAndRsi, out oldPriceAndRsi))
            {
                m_cryptoPriceAndRsiQueue.Enqueue(currentPriceAndRsi);
                m_currentTime = await m_systemClock.Wait(cancellationToken, currency, s_timeToWaitInSeconds, "RSI And Price",
                    m_currentTime);
                currentPriceAndRsi = m_currencyDataProvider.GetRsiAndClosePrice(currency, m_candleSizeInMinutes, m_currentTime);
            }
            
            var rsiAndPricePollingResponse = new PriceAndRsiPollingResponse(m_currentTime, oldPriceAndRsi ,currentPriceAndRsi);
            string message =
                $"{currency}: {nameof(PriceAndRsiCryptoPolling)} done, {rsiAndPricePollingResponse}";
            m_notificationService.Notify(message);
            return rsiAndPricePollingResponse;
        }
        
        private bool ShouldContinuePolling(PriceAndRsi priceAndRsi, out PriceAndRsi oldPriceAndRsi)
        {
            oldPriceAndRsi = null;
            if (priceAndRsi.Rsi > m_maxRsiToNotify)
            {
                return true;
            }
            oldPriceAndRsi = m_cryptoPriceAndRsiQueue.GetLowerRsiAndHigherPrice(priceAndRsi);
            if (oldPriceAndRsi is null)
            {
                return true;
            }
            
            s_logger.LogDebug($"Current PriceAndRsi: {priceAndRsi}, " +
                              $"OldPriceAndRsi : {oldPriceAndRsi}");
            return false;
        }

        private static string StartPollingDescription(string currency) =>
            $"{currency}: {nameof(PriceAndRsiCryptoPolling)} start, " +
            $"Get update every  {s_timeToWaitInSeconds / 60} minutes";
        
        private static string EndPollingDescription(string currency, PollingResponseBase pollingResponse) => 
            $"{currency}: {nameof(PriceAndRsiCryptoPolling)} done, {pollingResponse}";
        
        private PollingResponseBase CreateExceptionPollingResponse(Exception exception) => 
            new PriceAndRsiPollingResponse(m_currentTime, null ,null, false, exception);

        private PollingResponseBase CreateGotCancelledPollingResponse() => 
            new PriceAndRsiPollingResponse(m_currentTime, null ,null, true);
    }
}
    