﻿using System;
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
    public class RsiCryptoPolling : ICryptoPolling
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<RsiCryptoPolling>();
        private static readonly int s_timeToWaitInSeconds = 60;

        private readonly ICurrencyDataProvider m_currencyDataProvider;
        private readonly ISystemClock m_systemClock;
        private readonly INotificationService m_notificationService;
        private readonly decimal m_maxRsiToNotify;

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

        public async Task<IPollingResponse> StartAsync(string symbol, CancellationToken cancellationToken, DateTime currentTime)
        {
            s_logger.LogDebug($"{symbol}: {nameof(RsiCryptoPolling)}, " +
                              $"Get update every {s_timeToWaitInSeconds / 60} minutes");
            decimal rsi = m_currencyDataProvider.GetRsi(symbol, currentTime);
            while (rsi >= m_maxRsiToNotify)
            {
                currentTime = await m_systemClock.Wait(cancellationToken, symbol, s_timeToWaitInSeconds, "RSI", currentTime);
                rsi = m_currencyDataProvider.GetRsi(symbol, currentTime);
            }
            
            var rsiPollingResponse = new RsiPollingResponse(currentTime, rsi);
            string message = $"{symbol}: {nameof(RsiCryptoPolling)} done, {rsiPollingResponse}";
            m_notificationService.Notify(message);
            s_logger.LogDebug(message);
            return rsiPollingResponse;
        }
    }
}