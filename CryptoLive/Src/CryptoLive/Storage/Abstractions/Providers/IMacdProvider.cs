using System;

namespace Storage.Abstractions.Providers
{
    public interface IMacdProvider
    {
        decimal Get(string currency, DateTime currentTime);
    }
}