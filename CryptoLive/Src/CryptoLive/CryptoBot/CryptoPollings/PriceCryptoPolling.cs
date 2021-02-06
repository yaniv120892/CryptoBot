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
    public class PriceCryptoPolling : ICryptoPolling
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<PriceCryptoPolling>();

        private readonly ICurrencyDataProvider m_currencyDataProvider;
        private readonly ISystemClock m_systemClock;
        private readonly INotificationService m_notificationService;
        private readonly int m_delayTimeInSeconds;
        private readonly int m_priceChangeInPercentage;

        public PriceCryptoPolling(INotificationService notificationService,
            ICurrencyDataProvider currencyDataProvider, 
            ISystemClock systemClock,
            int delayTimeInSeconds,
            int priceChangeInPercentage)
        {
            m_notificationService = notificationService;
            m_currencyDataProvider = currencyDataProvider;
            m_systemClock = systemClock;
            m_delayTimeInSeconds = delayTimeInSeconds;
            m_priceChangeInPercentage = priceChangeInPercentage;
        }

        public async Task<IPollingResponse> StartAsync(string symbol, CancellationToken cancellationToken, DateTime currentTime)
        {
            decimal initialPrice = await m_currencyDataProvider.GetPriceAsync(symbol, currentTime);
            decimal minPrice = initialPrice * (100 - m_priceChangeInPercentage) / 100;
            decimal maxPrice = initialPrice * (100 + m_priceChangeInPercentage) / 100;
            s_logger.LogDebug($"{symbol}: {nameof(PriceCryptoPolling)} start, " +
                              $" {minPrice}-{maxPrice}, " +
                              $"Get update every {m_delayTimeInSeconds} seconds");
            
            decimal currentPrice = initialPrice;
            while (minPrice < currentPrice && currentPrice < maxPrice )
            {
                currentTime = await m_systemClock.Wait(cancellationToken, symbol, m_delayTimeInSeconds, "Price", currentTime);
                currentPrice = await m_currencyDataProvider.GetPriceAsync(symbol, currentTime);
            }

            var pricePollingResponse = new PricePollingResponse(initialPrice, currentPrice, currentTime);
            string message =
                $"{symbol}: {nameof(PriceCryptoPolling)} done, {pricePollingResponse}";
            m_notificationService.Notify(message);
            s_logger.LogDebug(message);
            return pricePollingResponse;
        }
    }
}