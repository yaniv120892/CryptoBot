using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using CryptoBot.Abstractions;
using CryptoBot.Abstractions.Factories;
using Infra;
using Microsoft.Extensions.Logging;
using Utils.Converters;

namespace CryptoBot
{
    public class CurrencyBot : ICurrencyBot
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<CurrencyBot>();
        private static readonly int s_waitInSecondsBeforeStartChild = 60;
        private static readonly int s_waitInSecondsAfterCandleNotRed = 60;
        private static readonly int s_waitInSecondsBeforeCandleIsGreen = (15-1)*60;
        
        private readonly ICurrencyBotPhasesExecutor m_currencyBotPhasesExecutor;
        private readonly ICurrencyBotFactory m_currencyBotFactory;
        private readonly INotificationService m_notificationService;
        private readonly CancellationTokenSource m_cancellationTokenSource;
        private readonly List<string> m_phasesDescription;
        private readonly int m_age;
        private readonly string m_currency;

        private DateTime m_currentTime;

        public CurrencyBot(ICurrencyBotFactory currencyBotFactory,
            INotificationService notificationService,
            ICurrencyBotPhasesExecutor currencyBotPhasesExecutor,
            string currency,
            CancellationTokenSource cancellationTokenSource,
            DateTime botStartTime,
            int age = 0)
        {
            m_currency = currency;
            m_currencyBotFactory = currencyBotFactory;
            m_notificationService = notificationService;
            m_cancellationTokenSource = cancellationTokenSource;
            m_currentTime = botStartTime;
            m_age = age;
            m_currencyBotPhasesExecutor = currencyBotPhasesExecutor;
            m_phasesDescription = new List<string>();
        }
        
        public async Task<(BotResultDetails, DateTime)> StartAsync()
        {
            return await StartAsyncImpl();
        }

        private BotResultDetails CreateBotResultDetails(int res)
        {
            BotResult botResult = BotResultConverter.ConvertIntToBotResult(res);
            return new BotResultDetails(botResult, m_phasesDescription);
        }

        private async Task<(BotResultDetails, DateTime)> StartAsyncImpl()
        {
            int phaseNumber = 0;
            s_logger.LogInformation($"{m_currency}_{m_age}: Start iteration");
            
            //m_currentTime = await m_currencyBotPhasesExecutor.WaitUntilRsiIsBelowMaxValueAsync(m_currentTime, m_cancellationTokenSource.Token, m_currency , m_age, ++phaseNumber, m_phasesDescription);
            m_currentTime = await m_currencyBotPhasesExecutor.WaitUntilLowerPriceAndHigherRsiAsync(m_currentTime, m_cancellationTokenSource.Token, m_currency , m_age, ++phaseNumber, m_phasesDescription);

            // Validator
            bool isCandleRed = m_currencyBotPhasesExecutor.ValidateCandleIsRed(m_currentTime, m_currency, m_age, ++phaseNumber, m_phasesDescription);
            if(!isCandleRed)
            {
                s_logger.LogInformation($"{m_currency}_{m_age}: Done iteration - candle is not red");
                m_currentTime = await m_currencyBotPhasesExecutor.WaitAsync(m_currentTime, m_cancellationTokenSource.Token, m_currency, s_waitInSecondsAfterCandleNotRed, "FullMode_WaitAfterCandleNotRed");
                return (CreateBotResultDetails(0), m_currentTime);
            }
            m_currentTime = await m_currencyBotPhasesExecutor.WaitAsync(m_currentTime, 
                m_cancellationTokenSource.Token,
                m_currency, 
                s_waitInSecondsBeforeStartChild, 
                "FullMode_WaitBeforeStartChild");
            Task<(BotResultDetails, DateTime)> child = StartChildAsync();
            m_currentTime = await m_currencyBotPhasesExecutor.WaitAsync(m_currentTime, 
                m_cancellationTokenSource.Token,
                m_currency,
                s_waitInSecondsBeforeCandleIsGreen, 
                "FullMode_WaitAfterCandleIsRed");
            
            bool isCandleGreen = m_currencyBotPhasesExecutor.ValidateCandleIsGreen(m_currentTime, m_currency, m_age, ++phaseNumber, m_phasesDescription);
            if(!isCandleGreen)
            {
                s_logger.LogInformation($"{m_currency}_{m_age}: Done iteration - candle is not green");
                return await child;
            }

            m_cancellationTokenSource.Cancel();
            // Enter
            decimal basePrice = m_currencyBotPhasesExecutor.GetPrice(m_currency, m_currentTime);
            m_notificationService.Notify($"{string.Join("\n\n",m_phasesDescription)}\n\n" +
                                         $"\tBot buy {m_currency} at price: {basePrice}");
            bool isWin;
            (isWin, m_currentTime) = await m_currencyBotPhasesExecutor.WaitUnitPriceChangeAsync(m_currentTime, CancellationToken.None, 
                m_currency, basePrice, m_age, ++phaseNumber, m_phasesDescription);
            m_notificationService.Notify(m_phasesDescription.Last());
            if (isWin)
            {
                s_logger.LogInformation($"{m_currency}_{m_age}: Done iteration - Win {m_currentTime}");
                return (CreateBotResultDetails(1), m_currentTime);
            }

            s_logger.LogInformation($"{m_currency}_{m_age}: Done iteration - Loss {m_currentTime}");
            return (CreateBotResultDetails(-1), m_currentTime);
        }
        
        private async Task<(BotResultDetails, DateTime)> StartChildAsync()
        {
            s_logger.LogDebug($"{m_currency}_{m_age}: Start child {m_currentTime}");
            ICurrencyBot childCurrencyBot = m_currencyBotFactory.Create(m_currency, m_cancellationTokenSource, m_currentTime, m_age+1);
            return await childCurrencyBot.StartAsync();
        }
    }
}