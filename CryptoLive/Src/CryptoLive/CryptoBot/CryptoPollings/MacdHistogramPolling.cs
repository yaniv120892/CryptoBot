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
    public class MacdHistogramCryptoPolling : CryptoPollingBase
    {
        private static readonly int s_timeToWaitInSeconds = 60;
        private static readonly string s_actionName = "Macd polling";
        
        private readonly ICurrencyDataProvider m_currencyDataProvider;
        private readonly ISystemClock m_systemClock;
        private readonly INotificationService m_notificationService;
        private readonly int m_maxMacdPollingTimeInMinutes;

        public MacdHistogramCryptoPolling(INotificationService notificationService,
            ICurrencyDataProvider currencyDataProvider, 
            ISystemClock systemClock,
            int maxMacdPollingTimeInMinutes)
        {
            m_notificationService = notificationService;
            m_currencyDataProvider = currencyDataProvider;
            m_systemClock = systemClock;
            m_maxMacdPollingTimeInMinutes = maxMacdPollingTimeInMinutes;
            PollingType = nameof(MacdHistogramCryptoPolling);
        }
        
        protected override async Task<PollingResponseBase> StartAsyncImpl(CancellationToken cancellationToken)
        {
            decimal macdHistogram =
                m_currencyDataProvider.GetMacdHistogram(Currency, CurrentTime);
            for (int i = 0; i < m_maxMacdPollingTimeInMinutes && macdHistogram < 0; i++)
            {
                CurrentTime = await m_systemClock.Wait(cancellationToken, Currency, s_timeToWaitInSeconds,
                    s_actionName,
                    CurrentTime);
                macdHistogram =
                    m_currencyDataProvider.GetMacdHistogram(Currency, CurrentTime);
            }

            if (macdHistogram >= 0)
            {
                MacdHistogramPollingResponse macdHistogramPollingResponse =
                        new MacdHistogramPollingResponse(CurrentTime, macdHistogram);
                    string message =
                        $"{Currency}: {s_actionName} done, {macdHistogramPollingResponse}";
                    m_notificationService.Notify(message);
                    return macdHistogramPollingResponse;
            }

            return CreateReachedMaxTimeMacdHistogramPollingResponse();
        }

        protected override string StartPollingDescription() =>
            $"{Currency}: {nameof(MacdHistogramCryptoPolling)} start, " +
            $"Get update every 1 minute," +
            $"Max iteration {m_maxMacdPollingTimeInMinutes}";

        protected override PollingResponseBase CreateGotCancelledPollingResponse() => 
            new MacdHistogramPollingResponse(CurrentTime, 0, false, true);

        protected override PollingResponseBase CreateExceptionPollingResponse(Exception e) => 
            new MacdHistogramPollingResponse(CurrentTime, 0, false, false, e);
        
        private PollingResponseBase CreateReachedMaxTimeMacdHistogramPollingResponse() => 
            new MacdHistogramPollingResponse(CurrentTime, 0, true);
    }
}