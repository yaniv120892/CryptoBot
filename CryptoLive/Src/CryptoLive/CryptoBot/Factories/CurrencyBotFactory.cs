using System;
using System.Threading;
using CryptoBot.Abstractions;

namespace CryptoBot.Factories
{
    public class CurrencyBotFactory : ICurrencyBotFactory
    {
        private readonly ICurrencyBotPhasesExecutor m_currencyBotPhasesExecutor;

        public CurrencyBotFactory(ICurrencyBotPhasesExecutor currencyBotPhasesExecutor)
        {
            m_currencyBotPhasesExecutor = currencyBotPhasesExecutor;
        }

        public CurrencyBot Create(string currency,
            CancellationTokenSource cancellationTokenSource,
            DateTime botStartTime) =>
            new CurrencyBot(m_currencyBotPhasesExecutor, currency, cancellationTokenSource, botStartTime);
    }
}