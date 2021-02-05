using System;

namespace CryptoBot.Abstractions
{
    public interface ICryptoValidator
    {
        bool Validate(string symbol, DateTime time);
    }
}