using System;

namespace Storage.Abstractions
{
    public interface IMacdProvider
    {
        decimal Get(string currency, DateTime currentTime);
    }
}