using System;

namespace CryptoBot.Abstractions
{
    public interface ICryptoValidator
    {
        bool Validate(string currency, int candleSize, DateTime currentTime);
    }
}