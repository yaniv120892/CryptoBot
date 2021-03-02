using System;
using System.Threading;
using CryptoBot.Abstractions;
using CryptoBot.Abstractions.Factories;

namespace CryptoBot.Factories
{
    public class CurrencyBotFactory : ICurrencyBotFactory
    {
        private readonly ICurrencyBotPhasesExecutor m_currencyBotPhasesExecutor;

        public CurrencyBotFactory(ICurrencyBotPhasesExecutor currencyBotPhasesExecutor)
        {
            m_currencyBotPhasesExecutor = currencyBotPhasesExecutor;
        }

        public ICurrencyBot Create(string currency,
            CancellationTokenSource cancellationTokenSource,
            DateTime botStartTime, 
            int age=0) 
            => new CurrencyBot(this, 
                m_currencyBotPhasesExecutor, 
                currency, 
                cancellationTokenSource, 
                botStartTime,
                age);
    }
}