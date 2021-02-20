using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Abstractions;
using Common.PollingResponses;
using CryptoBot.Abstractions;
using Infra;
using Storage.Abstractions.Providers;
using Utils.Abstractions;

namespace CryptoBot.CryptoPollings
{
    public class RsiCryptoPolling : CryptoPollingBase
    {
        private static readonly int s_timeToWaitInSeconds = 60;

        private readonly ICurrencyDataProvider m_currencyDataProvider;
        private readonly ISystemClock m_systemClock;
        private readonly INotificationService m_notificationService;
        private readonly decimal m_maxRsiToNotify;
        
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
            PollingType = nameof(RsiCryptoPolling);
        }
        
        protected override async Task<PollingResponseBase> StartAsyncImpl(CancellationToken cancellationToken)
        {
            decimal rsi = m_currencyDataProvider.GetRsi(Currency, CurrentTime);
            while (rsi >= m_maxRsiToNotify)
            {
                CurrentTime = await m_systemClock.Wait(cancellationToken, Currency, s_timeToWaitInSeconds,
                    s_actionName, CurrentTime);
                rsi = m_currencyDataProvider.GetRsi(Currency, CurrentTime);
            }

            var rsiPollingResponse = new RsiPollingResponse(CurrentTime, rsi);
            string notificationMessage = $"{Currency}: {nameof(RsiCryptoPolling)} done, {rsiPollingResponse}";
            m_notificationService.Notify(notificationMessage);
            return rsiPollingResponse;
        }
        
        protected override PollingResponseBase CreateExceptionPollingResponse(Exception exception) => 
            new RsiPollingResponse(CurrentTime, -1, false, exception);

        protected override PollingResponseBase CreateGotCancelledPollingResponse() => 
            new RsiPollingResponse(CurrentTime, -1, true);

        protected override string StartPollingDescription() => 
            $"{PollingType} {Currency} {CurrentTime}, Get update every {s_timeToWaitInSeconds / 60} minutes";
    }
}