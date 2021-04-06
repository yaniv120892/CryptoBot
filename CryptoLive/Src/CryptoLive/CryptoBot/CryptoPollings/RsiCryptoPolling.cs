using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
    public class RsiCryptoPolling : CryptoPollingBase
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<RsiCryptoPolling>();
        private static readonly int s_timeToWaitInSeconds = 60;
        private static string s_actionName = "RSI";

        private readonly ICurrencyDataProvider m_currencyDataProvider;
        private readonly ISystemClock m_systemClock;
        private readonly decimal m_maxRsiToNotify;
        private readonly Queue<CancellationToken> m_parentRunningCancellationToken;
        private readonly int m_iterationUntilWaitForParentCancellationToken;

        private int m_doneIterations;

        public RsiCryptoPolling(ICurrencyDataProvider currencyDataProvider,
            ISystemClock systemClock,
            decimal maxRsiToNotify,
            Queue<CancellationToken> parentRunningCancellationToken,
            int iterationUntilWaitForParentCancellationToken)
        {
            m_currencyDataProvider = currencyDataProvider;
            m_systemClock = systemClock;
            m_maxRsiToNotify = maxRsiToNotify;
            m_parentRunningCancellationToken = parentRunningCancellationToken;
            m_iterationUntilWaitForParentCancellationToken = iterationUntilWaitForParentCancellationToken;
            PollingType = nameof(RsiCryptoPolling);
        }

        protected override async Task<PollingResponseBase> StartAsyncImpl(CancellationToken cancellationToken)
        {
            decimal rsi = m_currencyDataProvider.GetRsi(Currency, CurrentTime);
            while (rsi > m_maxRsiToNotify)
            {
                CurrentTime = await m_systemClock.Wait(cancellationToken, Currency, s_timeToWaitInSeconds, s_actionName,
                    CurrentTime);
                await WaitForParentToFinishIfNeeded(cancellationToken);
                rsi = m_currencyDataProvider.GetRsi(Currency, CurrentTime);
                s_logger.LogTrace($"{Currency}: {rsi}");
            }
            
            var rsiAndPricePollingResponse = new RsiPollingResponse(CurrentTime, rsi);
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

        protected override string StartPollingDescription() =>
            $"{PollingType} {Currency} {CurrentTime:dd/MM/yyyy HH:mm:ss} start, " +
            $"Get update every  {s_timeToWaitInSeconds / 60} minutes";

        protected override PollingResponseBase CreateExceptionPollingResponse(Exception exception) => 
            new PriceAndRsiPollingResponse(CurrentTime, null ,null, false, exception);

        protected override PollingResponseBase CreateGotCancelledPollingResponse() => 
            new PriceAndRsiPollingResponse(CurrentTime, null ,null, true);
    }
}