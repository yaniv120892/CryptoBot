using System;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Abstractions;
using Common.PollingResponses;
using CryptoBot.Abstractions;
using Storage.Abstractions.Providers;
using Utils.Abstractions;

namespace CryptoBot.CryptoPollings
{
    public class CandleCryptoPolling : CryptoPollingBase
    {
        private static string s_actionName = "Candle polling";

        private readonly ICurrencyDataProvider m_currencyDataProvider;
        private readonly ISystemClock m_systemClock;
        private readonly decimal m_minPrice;
        private readonly decimal m_maxPrice;
        private readonly int m_candleSize;

        private MyCandle m_currCandle;

        public CandleCryptoPolling(ICurrencyDataProvider currencyDataProvider, 
            ISystemClock systemClock,
            int candleSize,
            decimal minPrice,
            decimal maxPrice)
        {
            m_currencyDataProvider = currencyDataProvider;
            m_systemClock = systemClock;
            m_candleSize = candleSize;
            m_minPrice = minPrice;
            m_maxPrice = maxPrice;
            PollingType = nameof(CandleCryptoPolling);
        }

        protected override async Task<PollingResponseBase> StartAsyncImpl(CancellationToken cancellationToken)
        {
            m_currCandle = m_currencyDataProvider.GetLastCandle(Currency, m_candleSize, CurrentTime);
            (bool isBelow, bool isAbove) = IsCandleInRange(m_currCandle);
            while (isBelow == false && isAbove == false)
            {
                CurrentTime = await m_systemClock.Wait(cancellationToken, Currency, DelayTimeInSeconds,
                    s_actionName, CurrentTime);
                m_currCandle = m_currencyDataProvider.GetLastCandle(Currency, m_candleSize, CurrentTime);
                (isBelow, isAbove) = IsCandleInRange(m_currCandle);
            }
            var candlePollingResponse = new CandlePollingResponse(isBelow, isAbove, CurrentTime, m_currCandle);
            return candlePollingResponse;
        }
        
        protected override string StartPollingDescription() =>
            $"{PollingType} {Currency} {CurrentTime} start, " +
            $"Get update every {m_candleSize} minutes"; 

        private (bool isBelow, bool isAbove) IsCandleInRange(MyCandle currCandle) =>
            (currCandle.Low < m_minPrice, currCandle.High > m_maxPrice);

        private int DelayTimeInSeconds => m_candleSize * 60;
        
        protected override PollingResponseBase CreateExceptionPollingResponse(Exception e) => 
            new CandlePollingResponse(false, false, CurrentTime, m_currCandle, false, e);

        protected override PollingResponseBase CreateGotCancelledPollingResponse() => 
            new CandlePollingResponse(false, false, CurrentTime, m_currCandle, true);
    }
}