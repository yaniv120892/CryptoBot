using System;
using System.Threading;
using Common;
using Common.Abstractions;
using CryptoBot.Abstractions;
using CryptoBot.Abstractions.Factories;
using Infra;

namespace CryptoBot.Factories
{
    public class CurrencyBotFactory : ICurrencyBotFactory
    {
        private readonly ICurrencyBotPhasesExecutor m_currencyBotPhasesExecutor;
        private readonly INotificationService m_notificationService;
        private readonly IAccountQuoteProvider m_accountQuoteProvider;

        public CurrencyBotFactory(ICurrencyBotPhasesExecutor currencyBotPhasesExecutor,
            INotificationService notificationService,
            IAccountQuoteProvider accountQuoteProvider)
        {
            m_currencyBotPhasesExecutor = currencyBotPhasesExecutor;
            m_notificationService = notificationService;
            m_accountQuoteProvider = accountQuoteProvider;
        }

        public ICurrencyBot Create(ICryptoPriceAndRsiQueue<PriceAndRsi> queue, string currency,
            CancellationTokenSource cancellationTokenSource,
            DateTime botStartTime, int age)
            => new CurrencyBot(this, 
                m_notificationService,
                m_currencyBotPhasesExecutor, 
                currency, 
                cancellationTokenSource, 
                botStartTime,
                queue,
                m_accountQuoteProvider,
                age);
    }
}