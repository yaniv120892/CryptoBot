using System;

namespace Storage.Abstractions
{
    public interface IMacdProvider
    {
        decimal Get(string symbol, DateTime currentTime);
    }
}