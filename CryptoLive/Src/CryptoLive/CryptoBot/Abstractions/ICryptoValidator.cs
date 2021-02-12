using System;

namespace CryptoBot.Abstractions
{
    public interface ICryptoValidator
    {
        bool Validate(string currency, DateTime time);
    }
}