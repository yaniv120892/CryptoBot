using System;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.PollingResponses;
using Infra;
using Microsoft.Extensions.Logging;
using Utils.Abstractions;

namespace Utils.CryptoPollings
{
    public class PricePolling : IPolling
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<PricePolling>();

        private readonly ICurrencyService m_currencyService;
        private readonly ISystemClock m_systemClock;
        private readonly INotificationHandler m_notificationHandler;
        private readonly int m_delayTimeInSeconds;

        public PricePolling(INotificationHandler notificationHandler, 
            ICurrencyService currencyService, 
            ISystemClock systemClock,
            int delayTimeInSeconds)
        {
            m_notificationHandler = notificationHandler;
            m_currencyService = currencyService;
            m_systemClock = systemClock;
            m_delayTimeInSeconds = delayTimeInSeconds;
        }

        public async Task<IPollingResponse> StartPolling(string desiredSymbol, CancellationToken cancellationToken, DateTime currentTime)
        {
            s_logger.LogInformation("Start crypto price polling, Get update every {m_delayTimeInSeconds} seconds");
            decimal initialPrice = await m_currencyService.GetPriceAsync(desiredSymbol, currentTime);
            decimal currentPrice = initialPrice;
            while (!m_notificationHandler.NotifyIfNeeded(currentPrice, desiredSymbol))
            {
                currentTime = await m_systemClock.Wait(cancellationToken, desiredSymbol, m_delayTimeInSeconds, "Price", currentTime);
                currentPrice = await m_currencyService.GetPriceAsync(desiredSymbol, currentTime);
            }

            s_logger.LogInformation("Done crypto price polling");
            return new PricePollingResponse(initialPrice,currentPrice, currentTime);
        }
    }
}