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
    public class CandlePolling : IPolling
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<CandlePolling>();
        
        private readonly int m_delayTimeInSeconds;
        private readonly ICurrencyService m_currencyService;
        private readonly ISystemClock m_systemClock;
        private readonly INotificationHandler m_notificationHandler;
        private readonly int m_candleSize;

        public CandlePolling(INotificationHandler notificationHandler, 
            ICurrencyService currencyService, 
            ISystemClock systemClock,
            int delayTimeInSeconds, 
            int candleSize)
        {
            if (delayTimeInSeconds / 60 != candleSize)
            {
                s_logger.LogError($"delayTimeInSeconds/60=candleSize should be true but delayTimeInSeconds={delayTimeInSeconds},candleSize={candleSize}");
                throw new ArgumentException(
                    $"delayTimeInSeconds/60=candleSize should be true but delayTimeInSeconds={delayTimeInSeconds},candleSize={candleSize}");
            }
            m_notificationHandler = notificationHandler;
            m_currencyService = currencyService;
            m_systemClock = systemClock;
            m_delayTimeInSeconds = delayTimeInSeconds;
            m_candleSize = candleSize;
        }

        public async Task<IPollingResponse> StartPolling(string desiredSymbol, CancellationToken cancellationToken, DateTime currentTime)
        {
            s_logger.LogInformation($"{desiredSymbol}: Start crypto candle polling, Get update every {m_delayTimeInSeconds / 60} minutes");
            (MyCandle _, MyCandle currCandle) = await m_currencyService.GetLastCandlesAsync(desiredSymbol, m_candleSize, currentTime);
            (bool isBelow, bool isAbove) = IsCandleInRange(currCandle, desiredSymbol);
            while (isBelow == false && isAbove == false)
            {
                currentTime = await m_systemClock.Wait(cancellationToken, desiredSymbol, m_delayTimeInSeconds, "Price range", currentTime);
                (_, currCandle) = await m_currencyService.GetLastCandlesAsync(desiredSymbol, 1, currentTime);
                (isBelow, isAbove) = IsCandleInRange(currCandle, desiredSymbol);
            }

            s_logger.LogInformation($"{desiredSymbol}: Done crypto candle polling");
            return new CandlePollingResponse(isBelow, isAbove, currentTime);
        }

        private (bool isBelow, bool isAbove) IsCandleInRange(MyCandle currCandle, string symbol)
        {
            if (m_notificationHandler.NotifyIfNeeded(currCandle.Low, symbol))
            {
                return (true, false);
            }
            
            return m_notificationHandler.NotifyIfNeeded(currCandle.High, symbol) ? 
                (false, true) : 
                (false, false);
        }
    }
}