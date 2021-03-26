using System;
using System.Collections.Generic;
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

        public ICurrencyBot Create(ICryptoPriceAndRsiQueue<PriceAndRsi> queue,
            Queue<CancellationToken> parentRunningCancellationToken,
            string currency,
            CancellationTokenSource cancellationTokenSource,
            DateTime botStartTime, 
            int age)
        {
            var currencyBot = new CurrencyBot(this,
                m_notificationService,
                m_currencyBotPhasesExecutor,
                currency,
                cancellationTokenSource,
                botStartTime,
                queue,
                m_accountQuoteProvider,
                CreateCloneQueue(parentRunningCancellationToken),
                age);
            return currencyBot;
        }

        private static Queue<CancellationToken> CreateCloneQueue(Queue<CancellationToken> parentRunningCancellationToken)
        {
            var cloneQueue = new Queue<CancellationToken>();
            if (parentRunningCancellationToken.Count == 0)
            {
                return cloneQueue;
            }
            var arr = new CancellationToken[parentRunningCancellationToken.Count];
            parentRunningCancellationToken.CopyTo(arr, 0);
            int i = 0;
            while (i < arr.Length)
            {
                cloneQueue.Enqueue(arr[i]);
                i++;
            }
            return cloneQueue;
        }
    }
}