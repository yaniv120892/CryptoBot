using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Abstractions;
using Infra;
using Microsoft.Extensions.Logging;

namespace CryptoBot.Abstractions
{
    public abstract class CryptoPollingBase : ICryptoPolling
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<CryptoPollingBase>();
        
        protected DateTime CurrentTime;
        protected string Currency;
        protected string PollingType;

        public async Task<PollingResponseBase> StartAsync(string currency, CancellationToken cancellationToken, DateTime currentTime)
        {
            PollingResponseBase pollingResponse;
            CurrentTime = currentTime;
            Currency = currency;
            s_logger.LogDebug(StartPollingDescription());

            try
            {
                pollingResponse =  await StartAsyncImpl(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                s_logger.LogWarning($"{PollingType}_{Currency}_{CurrentTime} - got cancellation request");
                pollingResponse = CreateGotCancelledPollingResponse();
            }
            catch (Exception e)
            {
                s_logger.LogWarning(e, $"{PollingType}_{Currency}_{CurrentTime} - Failed, {e.Message}");
                pollingResponse = CreateExceptionPollingResponse(e);
            }
            s_logger.LogDebug(EndPollingDescription(pollingResponse));
            return pollingResponse;
        }

        protected abstract PollingResponseBase CreateExceptionPollingResponse(Exception exception);
        protected abstract PollingResponseBase CreateGotCancelledPollingResponse();
        protected abstract Task<PollingResponseBase> StartAsyncImpl(CancellationToken cancellationToken);
        protected abstract string StartPollingDescription();
        private string EndPollingDescription(PollingResponseBase pollingResponse) =>
            $"{PollingType} {Currency} {CurrentTime}: done, {pollingResponse}";    }
}