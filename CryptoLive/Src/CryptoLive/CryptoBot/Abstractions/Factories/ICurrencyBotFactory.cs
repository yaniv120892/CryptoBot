using System;
using System.Threading;
using Common;
using Common.Abstractions;

namespace CryptoBot.Abstractions.Factories
{
    public interface ICurrencyBotFactory
    {
        ICurrencyBot Create(ICryptoPriceAndRsiQueue<PriceAndRsi> queue,
            string currency,
            CancellationTokenSource cancellationTokenSource,
            DateTime botStartTime,
            int age = 0);
    }
}