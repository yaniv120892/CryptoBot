using System;
using System.Threading;

namespace CryptoBot.Abstractions.Factories
{
    public interface ICurrencyBotFactory
    {
        ICurrencyBot Create(string currency,
            CancellationTokenSource cancellationTokenSource,
            DateTime botStartTime,
            int age = 0);
    }
}