using System;
using Common.Abstractions;

namespace CryptoBot.Exceptions
{
    public class PollingResponseException : Exception
    {
        public PollingResponseBase PollingResponse { get; }

        public PollingResponseException(PollingResponseBase pollingResponse)
        {
            PollingResponse = pollingResponse;
        }
    }
}