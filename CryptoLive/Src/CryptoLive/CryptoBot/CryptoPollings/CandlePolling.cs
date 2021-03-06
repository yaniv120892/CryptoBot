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
    public class CandleCryptoPolling : CryptoPollingBase
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<CandleCryptoPolling>();
        private static string s_actionName = "Candle polling";

        private readonly int m_delayTimeInSeconds;
        private readonly ICurrencyDataProvider m_currencyDataProvider;
        private readonly ISystemClock m_systemClock;
        private readonly decimal m_minPrice;
        private readonly decimal m_maxPrice;
        
        private MyCandle m_currCandle;

        public CandleCryptoPolling(ICurrencyDataProvider currencyDataProvider, 
            ISystemClock systemClock,
            int delayTimeInSeconds, 
            int candleSize,
            decimal minPrice,
            decimal maxPrice)
        {
            if (delayTimeInSeconds / 60 != candleSize)
            {
                s_logger.LogError($"delayTimeInSeconds/60=candleSize should be true but delayTimeInSeconds={delayTimeInSeconds},candleSize={candleSize}");
                throw new ArgumentException(
                    $"delayTimeInSeconds/60=candleSize should be true but delayTimeInSeconds={delayTimeInSeconds},candleSize={candleSize}");
            }
            m_currencyDataProvider = currencyDataProvider;
            m_systemClock = systemClock;
            m_delayTimeInSeconds = delayTimeInSeconds;
            m_minPrice = minPrice;
            m_maxPrice = maxPrice;
            PollingType = nameof(CandleCryptoPolling);
        }

        protected override async Task<PollingResponseBase> StartAsyncImpl(CancellationToken cancellationToken)
        {
            m_currCandle = m_currencyDataProvider.GetLastCandle(Currency, CurrentTime);
            (bool isBelow, bool isAbove) = IsCandleInRange(m_currCandle);
            while (isBelow == false && isAbove == false)
            {
                CurrentTime = await m_systemClock.Wait(cancellationToken, Currency, m_delayTimeInSeconds,
                    s_actionName, CurrentTime);
                m_currCandle = m_currencyDataProvider.GetLastCandle(Currency, CurrentTime);
                (isBelow, isAbove) = IsCandleInRange(m_currCandle);
            }
            var candlePollingResponse = new CandlePollingResponse(isBelow, isAbove, CurrentTime, m_currCandle);
            return candlePollingResponse;
        }
        
        protected override string StartPollingDescription() =>
            $"{PollingType} {Currency} {CurrentTime} start, " +
            $"Get update every  {m_delayTimeInSeconds / 60} minutes"; 

        private (bool isBelow, bool isAbove) IsCandleInRange(MyCandle currCandle) =>
            (currCandle.Low < m_minPrice, currCandle.High > m_maxPrice);
        
        protected override PollingResponseBase CreateExceptionPollingResponse(Exception e) => 
            new CandlePollingResponse(false, false, CurrentTime, m_currCandle, false, e);

        protected override PollingResponseBase CreateGotCancelledPollingResponse() => 
            new CandlePollingResponse(false, false, CurrentTime, m_currCandle, true);
    }
}