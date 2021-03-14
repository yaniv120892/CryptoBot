using System;
using System.Threading;

namespace CryptoBot.Abstractions.Factories
{
    public interface ICurrencyBotFactory
    {
        ICurrencyBot Create(string currency,
            CancellationTokenSource cancellationTokenSource,
            DateTime botStartTime,
            decimal quoteOrderQuantity,
            int age = 0);
    }
}