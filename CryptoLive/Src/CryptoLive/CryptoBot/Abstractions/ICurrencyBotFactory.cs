using System;
using System.Threading;

namespace CryptoBot.Abstractions
{
    public interface ICurrencyBotFactory
    {
        CurrencyBot Create(string currency,
            CancellationTokenSource cancellationTokenSource,
            DateTime botStartTime);
    }
}