using System;

namespace Storage.Abstractions
{
    public interface IRsiProvider
    {
        decimal Get(string currency, DateTime dateTime);
    }
}