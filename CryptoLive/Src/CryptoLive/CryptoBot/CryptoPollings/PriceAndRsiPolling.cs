using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Abstractions;
using Common.PollingResponses;
using CryptoBot.Abstractions;
using CryptoExchange.Net;
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

        private readonly ICurrencyDataProvider m_currencyDataProvider;
        private readonly ISystemClock m_systemClock;
        private readonly ICryptoPriceAndRsiQueue<PriceAndRsi> m_cryptoPriceAndRsiQueue;
        private readonly decimal m_maxRsiToNotify;
        private readonly Queue<CancellationToken> m_parentRunningCancellationToken;
        private readonly int m_iterationUntilWaitForParentCancellationToken;

        private int m_doneIterations;

        public PriceAndRsiCryptoPolling(ICurrencyDataProvider currencyDataProvider,
            ISystemClock systemClock,
            ICryptoPriceAndRsiQueue<PriceAndRsi> cryptoPriceAndRsiQueue,
            decimal maxRsiToNotify,
            Queue<CancellationToken> parentRunningCancellationToken,
            int iterationUntilWaitForParentCancellationToken)
        {
            m_currencyDataProvider = currencyDataProvider;
            m_systemClock = systemClock;
            m_maxRsiToNotify = maxRsiToNotify;
            m_parentRunningCancellationToken = parentRunningCancellationToken;
            m_iterationUntilWaitForParentCancellationToken = iterationUntilWaitForParentCancellationToken;
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
                await WaitForParentToFinishIfNeeded(cancellationToken);
                currentPriceAndRsi = m_currencyDataProvider.GetRsiAndClosePrice(Currency, CurrentTime);
                s_logger.LogTrace($"{Currency}: {currentPriceAndRsi}");
            }
            
            var rsiAndPricePollingResponse = new PriceAndRsiPollingResponse(CurrentTime, oldPriceAndRsi ,currentPriceAndRsi);
            return rsiAndPricePollingResponse;
        }

        private async ValueTask WaitForParentToFinishIfNeeded(CancellationToken cancellationToken)
        {
            m_doneIterations++;
            int runningParentsCount = m_parentRunningCancellationToken.Count;
            if (runningParentsCount == 0 
                || runningParentsCount + m_doneIterations < m_iterationUntilWaitForParentCancellationToken)
            {
                return;
            }

            await m_parentRunningCancellationToken.Dequeue().WaitHandle.WaitOneAsync(int.MaxValue, cancellationToken);
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
                              $"Old PriceAndRsi : {oldPriceAndRsi}");
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
    