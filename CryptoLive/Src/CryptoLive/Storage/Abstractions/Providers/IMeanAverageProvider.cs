using System;

namespace Storage.Abstractions.Providers
{
    public interface IMeanAverageProvider
    {
        decimal Get(string currency, DateTime dateTime);
    }
}