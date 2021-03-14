using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Abstractions;
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
        private readonly decimal m_quoteOrderQuantity;

        public CurrencyBot(ICurrencyBotFactory currencyBotFactory,
            INotificationService notificationService,
            ICurrencyBotPhasesExecutor currencyBotPhasesExecutor,
            string currency,
            CancellationTokenSource cancellationTokenSource,
            DateTime botStartTime, 
            decimal quoteOrderQuantity, 
            int age = 0)
        {
            m_currency = currency;
            m_currencyBotFactory = currencyBotFactory;
            m_notificationService = notificationService;
            m_cancellationTokenSource = cancellationTokenSource;
            m_currentTime = botStartTime;
            m_quoteOrderQuantity = quoteOrderQuantity;
            m_age = age;
            m_currencyBotPhasesExecutor = currencyBotPhasesExecutor;
            m_phasesDescription = new List<string>();
        }
        
        public async Task<(BotResultDetails, DateTime)> StartAsync()
        {
            return await StartAsyncImpl();
        }

        private BotResultDetails CreateBotResultDetails(int res, decimal newQuoteOrderQuantity)
        {
            BotResult botResult = BotResultConverter.ConvertIntToBotResult(res);
            return new BotResultDetails(botResult, m_phasesDescription, newQuoteOrderQuantity);
        }

        private async Task<(BotResultDetails, DateTime)> StartAsyncImpl()
        {
            int phaseNumber = 0;
            s_logger.LogInformation($"{m_currency}_{m_age}: Start iteration");
            
            PollingResponseBase pollingResponseBase = await m_currencyBotPhasesExecutor.WaitUntilLowerPriceAndHigherRsiAsync(m_currentTime, 
                m_cancellationTokenSource.Token, m_currency , m_age, ++phaseNumber, m_phasesDescription);
            m_currentTime = pollingResponseBase.Time;
            if (!pollingResponseBase.IsSuccess)
            {
                return CreateNotSuccessBotResultDetails(pollingResponseBase);
            }
            // Validator
            bool isCandleRed = m_currencyBotPhasesExecutor.ValidateCandleIsRed(m_currentTime, m_currency, m_age, ++phaseNumber, m_phasesDescription);
            if(!isCandleRed)
            {
                s_logger.LogInformation($"{m_currency}_{m_age}: Done iteration - candle is not red");
                m_currentTime = await m_currencyBotPhasesExecutor.WaitAsync(m_currentTime, m_cancellationTokenSource.Token, m_currency, s_waitInSecondsAfterCandleNotRed, "FullMode_WaitAfterCandleNotRed");
                return (CreateBotResultDetails(0, m_quoteOrderQuantity), m_currentTime);
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
            BuyAndSellTradeInfo buyAndSellTradeInfo = await m_currencyBotPhasesExecutor.BuyAndPlaceSellOrder(m_currentTime,
                m_currency, m_age, ++phaseNumber, m_phasesDescription, m_quoteOrderQuantity);
            m_notificationService.Notify($"{string.Join("\n\n",m_phasesDescription)}\n\n" +
                                         $"\tBot buy {m_currency} at price: {buyAndSellTradeInfo.BuyPrice}");
            bool isWin;
            (isWin, pollingResponseBase) = await m_currencyBotPhasesExecutor.WaitUnitPriceChangeAsync(m_currentTime, CancellationToken.None, 
                m_currency, buyAndSellTradeInfo.BuyPrice, m_age, ++phaseNumber, m_phasesDescription);
            m_currentTime = pollingResponseBase.Time;
            if (!pollingResponseBase.IsSuccess)
            {
                return (CreateBotResultDetails(0, m_quoteOrderQuantity), m_currentTime);
            }

            decimal newQuoteOrderQuantity;
            m_notificationService.Notify(m_phasesDescription.LastOrDefault());
            if (isWin)
            {
                newQuoteOrderQuantity = buyAndSellTradeInfo.QuoteOrderQuantityOnWin;
                s_logger.LogInformation($"{m_currency}_{m_age}: Done iteration - Win , NewQuoteOrderQuantity: {newQuoteOrderQuantity:f4} {m_currentTime}");
                return (CreateBotResultDetails(1, newQuoteOrderQuantity), m_currentTime);
            }

            newQuoteOrderQuantity = buyAndSellTradeInfo.QuoteOrderQuantityOnLoss;
            s_logger.LogInformation($"{m_currency}_{m_age}: Done iteration - Loss, NewQuoteOrderQuantity: {newQuoteOrderQuantity:f4} {m_currentTime}");
            return (CreateBotResultDetails(-1, newQuoteOrderQuantity), m_currentTime);
        }

        private (BotResultDetails, DateTime) CreateNotSuccessBotResultDetails(PollingResponseBase pollingResponseBase)
        {
            if (pollingResponseBase.IsCancelled)
            {
                return (CreateBotResultDetails(-2, m_quoteOrderQuantity), m_currentTime);
            }

            if (pollingResponseBase.Exception != null)
            {
                return (CreateBotResultDetails(-2, m_quoteOrderQuantity), m_currentTime);
            }

            throw new Exception("Polling is not success but did not got cancellation request or exception");
        }

        private async Task<(BotResultDetails, DateTime)> StartChildAsync()
        {
            s_logger.LogDebug($"{m_currency}_{m_age}: Start child {m_currentTime}");
            ICurrencyBot childCurrencyBot = m_currencyBotFactory.Create(m_currency, m_cancellationTokenSource, m_currentTime, m_quoteOrderQuantity, m_age+1);
            return await childCurrencyBot.StartAsync();
        }
    }
}