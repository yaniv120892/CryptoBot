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

        public async Task<IPollingResponse> StartAsync(string symbol, CancellationToken cancellationToken,
            DateTime currentTime)
        {
            s_logger.LogDebug($"{symbol}: {nameof(PriceAndRsiCryptoPolling)} start, " +
                              $"Get update every  {s_timeToWaitInSeconds / 60} minutes");
            
            PriceAndRsi priceAndRsi =
                m_currencyDataProvider.GetRsiAndClosePrice(symbol, m_candleSizeInMinutes, currentTime);
            
            while (ShouldContinuePolling(priceAndRsi))
            {
                m_cryptoFixedSizeQueueImpl.Enqueue(priceAndRsi);
                currentTime = await m_systemClock.Wait(cancellationToken, symbol, s_timeToWaitInSeconds, "RSI And Price",
                    currentTime);
                priceAndRsi = m_currencyDataProvider.GetRsiAndClosePrice(symbol, m_candleSizeInMinutes, currentTime);
            }

            var rsiAndPricePollingResponse = new RsiAndPricePollingResponse(currentTime, priceAndRsi);
            string message =
                $"{symbol}: {nameof(PriceAndRsiCryptoPolling)} done, {rsiAndPricePollingResponse}";
            m_notificationService.Notify(message);
            s_logger.LogDebug(message);
            return rsiAndPricePollingResponse;
        }
        
        private bool ShouldContinuePolling(PriceAndRsi priceAndRsi)
        {
            if (priceAndRsi.Rsi > m_maxRsiToNotify)
            {
                return true;
            }
            PriceAndRsi oldPriceAndRsi = m_cryptoFixedSizeQueueImpl.GetLowerRsiAndHigherPrice(priceAndRsi);
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
    