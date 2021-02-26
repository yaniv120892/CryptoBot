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
    public class PriceAndRsiCryptoPolling : CryptoPollingBase
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<PriceAndRsiCryptoPolling>();
        private static readonly int s_timeToWaitInSeconds = 60;
        private static string s_actionName = "RSI And Price";

        private readonly INotificationService m_notificationService;
        private readonly ICurrencyDataProvider m_currencyDataProvider;
        private readonly ISystemClock m_systemClock;
        private readonly ICryptoPriceAndRsiQueue<PriceAndRsi> m_cryptoPriceAndRsiQueue;
        private readonly decimal m_maxRsiToNotify;
        
        public PriceAndRsiCryptoPolling(INotificationService notificationService,
            ICurrencyDataProvider currencyDataProvider,
            ISystemClock systemClock,
            ICryptoPriceAndRsiQueue<PriceAndRsi> cryptoPriceAndRsiQueue,
            decimal maxRsiToNotify)
        {
            m_notificationService = notificationService;
            m_currencyDataProvider = currencyDataProvider;
            m_systemClock = systemClock;
            m_maxRsiToNotify = maxRsiToNotify;
            m_cryptoPriceAndRsiQueue = cryptoPriceAndRsiQueue;
            PollingType = nameof(PriceAndRsiCryptoPolling);
        }

        protected override async Task<PollingResponseBase> StartAsyncImpl(CancellationToken cancellationToken)
        {
            PriceAndRsi currentPriceAndRsi =
                m_currencyDataProvider.GetRsiAndClosePrice(Currency, CurrentTime);
            PriceAndRsi oldPriceAndRsi;
            while (ShouldContinuePolling(currentPriceAndRsi, out oldPriceAndRsi))
            {
                m_cryptoPriceAndRsiQueue.Enqueue(currentPriceAndRsi);
                CurrentTime = await m_systemClock.Wait(cancellationToken, Currency, s_timeToWaitInSeconds, s_actionName,
                    CurrentTime);
                currentPriceAndRsi = m_currencyDataProvider.GetRsiAndClosePrice(Currency, CurrentTime);
                s_logger.LogInformation(currentPriceAndRsi.ToString());
            }
            
            var rsiAndPricePollingResponse = new PriceAndRsiPollingResponse(CurrentTime, oldPriceAndRsi ,currentPriceAndRsi);
            string message =
                $"{Currency}: {nameof(PriceAndRsiCryptoPolling)} done, {rsiAndPricePollingResponse}";
            m_notificationService.Notify(message);
            return rsiAndPricePollingResponse;
        }
        
        private bool ShouldContinuePolling(PriceAndRsi priceAndRsi, out PriceAndRsi oldPriceAndRsi)
        {
            oldPriceAndRsi = null;
            if (priceAndRsi.Rsi > m_maxRsiToNotify)
            {
                return true;
            }
            oldPriceAndRsi = m_cryptoPriceAndRsiQueue.GetLowerRsiAndHigherPrice(priceAndRsi);
            if (oldPriceAndRsi is null)
            {
                return true;
            }
            
            s_logger.LogDebug($"Current PriceAndRsi: {priceAndRsi}, " +
                              $"OldPriceAndRsi : {oldPriceAndRsi}");
            return false;
        }

        protected override string StartPollingDescription() =>
            $"{PollingType} {Currency} {CurrentTime} start, " +
            $"Get update every  {s_timeToWaitInSeconds / 60} minutes";

        protected override PollingResponseBase CreateExceptionPollingResponse(Exception exception) => 
            new PriceAndRsiPollingResponse(CurrentTime, null ,null, false, exception);

        protected override PollingResponseBase CreateGotCancelledPollingResponse() => 
            new PriceAndRsiPollingResponse(CurrentTime, null ,null, true);
    }
}
    