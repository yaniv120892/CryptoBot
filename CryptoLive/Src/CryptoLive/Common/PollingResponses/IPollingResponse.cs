using System;

namespace Common.PollingResponses
{
    public interface IPollingResponse
    {
        public DateTime Time { get; }
    }
}