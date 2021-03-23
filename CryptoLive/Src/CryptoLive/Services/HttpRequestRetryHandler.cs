using System;
using Infra;
using Microsoft.Extensions.Logging;

namespace Services
{
    public class RetryHandler
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<RetryHandler>();
        private static readonly int s_totalRetriesToDo = 3;
        
        private static TReturn RetryOnFailure<TReturn>(Func<TReturn> action)
        {
            Exception exceptionToThrowOnFailure = null;
            var nameOfMethodToRetry = action.Method.Name;
            var className = action.Method.ReflectedType?.Name;

            for (var retryIteration = 0; retryIteration <= s_totalRetriesToDo; retryIteration++)
            {
                try
                {
                    return action();
                }
                catch (Exception exception)
                {
                    exceptionToThrowOnFailure = exception;
                    s_logger.LogDebug(retryIteration == 0
                        ? $"First attempt to invoke method {className}.{nameOfMethodToRetry} failed"
                        : $"Retry number {retryIteration} on method {className}.{nameOfMethodToRetry} failed, Exception Message:{exception.Message}");

                    if (!ShouldRetry(exception))
                    {
                        throw;
                    }
                }
            }

            throw exceptionToThrowOnFailure;
        }

        private static bool ShouldRetry(Exception exception)
        {
            return false;
        }
    }
}