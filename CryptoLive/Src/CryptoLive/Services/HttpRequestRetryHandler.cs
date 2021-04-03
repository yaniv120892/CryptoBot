using System;
using System.Threading.Tasks;
using CryptoExchange.Net.Objects;
using Infra;
using Microsoft.Extensions.Logging;

namespace Services
{
    public class HttpRequestRetryHandler
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<HttpRequestRetryHandler>();
        private static readonly int s_totalRetriesToDo = 3;
        
        public static async Task<WebCallResult<T>> RetryOnFailure<T>(
            Func<Task<WebCallResult<T>>> function,
            string requestDescription)
        {
            Exception exceptionToThrowOnFailure = null;

            for (var retryIteration = 0; retryIteration <= s_totalRetriesToDo; retryIteration++)
            {
                try
                {
                    var response = await function();
                    ResponseHandler.AssertSuccessResponse(response, requestDescription);
                    return response;
                }
                catch (Exception exception)
                {
                    exceptionToThrowOnFailure = exception;
                    s_logger.LogDebug(retryIteration == 0
                        ? $"First attempt to invoke {requestDescription} failed, Exception Message:{exception.Message}"
                        : $"Retry number {retryIteration} on {requestDescription} failed, Exception Message:{exception.Message}");

                    if (!ShouldRetry(exception))
                    {
                        throw;
                    }
                }
            }

            s_logger.LogDebug("Reach to max number of retries");
            throw exceptionToThrowOnFailure;
        }

        private static bool ShouldRetry(Exception exception)
        {
            return false;
        }
    }
}