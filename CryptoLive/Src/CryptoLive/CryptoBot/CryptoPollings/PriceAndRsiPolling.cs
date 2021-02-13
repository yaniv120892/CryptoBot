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
    public class PriceAndRsiCryptoPolling : ICryptoPolling
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<PriceAndRsiCryptoPolling>();
        private static readonly int s_timeToWaitInSeconds = 60;

        private readonly INotificationService m_notificationService;
        private readonly ICurrencyDataProvider m_currencyDataProvider;
        private readonly ISystemClock m_systemClock;
        private readonly int m_candleSizeInMinutes;
        private readonly decimal m_maxRsiToNotify;
        private readonly CryptoFixedSizeQueueImpl<PriceAndRsi> m_cryptoFixedSizeQueueImpl;

        public PriceAndRsiCryptoPolling(INotificationService notificationService,
            ICurrencyDataProvider currencyDataProvider,
            ISystemClock systemClock,
            int candleSizeInMinutes,
            decimal maxRsiToNotify,
            int rsiMemorySize)
        {
            m_notificationService = notificationService;
            m_currencyDataProvider = currencyDataProvider;
            m_systemClock = systemClock;
            m_candleSizeInMinutes = candleSizeInMinutes;
            m_maxRsiToNotify = maxRsiToNotify;
            m_cryptoFixedSizeQueueImpl = new CryptoFixedSizeQueueImpl<PriceAndRsi>(rsiMemorySize);
        }

        public async Task<PollingResponseBase> StartAsync(string currency, CancellationToken cancellationToken,
            DateTime currentTime)
        {
            s_logger.LogDebug($"{currency}: {nameof(PriceAndRsiCryptoPolling)} start, " +
                              $"Get update every  {s_timeToWaitInSeconds / 60} minutes");
            
            PriceAndRsi currentPriceAndRsi =
                m_currencyDataProvider.GetRsiAndClosePrice(currency, m_candleSizeInMinutes, currentTime);
            PriceAndRsi oldPriceAndRsi;
            while (ShouldContinuePolling(currentPriceAndRsi, out oldPriceAndRsi))
            {
                m_cryptoFixedSizeQueueImpl.Enqueue(currentPriceAndRsi);
                currentTime = await m_systemClock.Wait(cancellationToken, currency, s_timeToWaitInSeconds, "RSI And Price",
                    currentTime);
                currentPriceAndRsi = m_currencyDataProvider.GetRsiAndClosePrice(currency, m_candleSizeInMinutes, currentTime);
            }

            var rsiAndPricePollingResponse = new RsiAndPricePollingResponse(currentTime, oldPriceAndRsi ,currentPriceAndRsi);
            string message =
                $"{currency}: {nameof(PriceAndRsiCryptoPolling)} done, {rsiAndPricePollingResponse}";
            m_notificationService.Notify(message);
            s_logger.LogDebug(message);
            return rsiAndPricePollingResponse;
        }
        
        private bool ShouldContinuePolling(PriceAndRsi priceAndRsi, out PriceAndRsi oldPriceAndRsi)
        {
            oldPriceAndRsi = null;
            if (priceAndRsi.Rsi > m_maxRsiToNotify)
            {
                return true;
            }
            oldPriceAndRsi = m_cryptoFixedSizeQueueImpl.GetLowerRsiAndHigherPrice(priceAndRsi);
            if (oldPriceAndRsi is null)
            {
                return true;
            }
            
            s_logger.LogDebug($"Current PriceAndRsi: {priceAndRsi}, " +
                              $"OldPriceAndRsi : {oldPriceAndRsi}");
            return false;
        }
    }
}
    