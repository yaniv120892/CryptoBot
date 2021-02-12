using System;

namespace Storage.Abstractions.Providers
{
    public interface IRsiProvider
    {
        decimal Get(string currency, DateTime dateTime);
    }
}