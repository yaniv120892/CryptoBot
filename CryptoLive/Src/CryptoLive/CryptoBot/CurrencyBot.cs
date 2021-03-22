using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Abstractions;
using CryptoBot.Abstractions;
using CryptoBot.Abstractions.Factories;
using CryptoBot.Exceptions;
using CryptoBot.Factories;
using Infra;
using Microsoft.Extensions.Logging;

namespace CryptoBot
{
    public class CurrencyBot : ICurrencyBot
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<CurrencyBot>();
        private static readonly int s_waitInSecondsAfterValidateReadCandle = 60;
        private static readonly int s_waitInSecondsBeforeValidateCandleIsGreen = (15-1)*60;

        private readonly ICurrencyBotPhasesExecutor m_currencyBotPhasesExecutor;
        private readonly ICryptoPriceAndRsiQueue<PriceAndRsi> m_cryptoPriceAndRsiQueue;
        private readonly ICurrencyBotFactory m_currencyBotFactory;
        private readonly INotificationService m_notificationService;
        private readonly IAccountQuoteProvider m_accountQuoteProvider;
        private readonly CancellationTokenSource m_cancellationTokenSource;
        private readonly int m_age;
        private readonly string m_currency;

        private DateTime m_currentTime;
        private int m_phaseNumber;
        private List<string> m_phasesDescription;

        public CurrencyBot(ICurrencyBotFactory currencyBotFactory,
            INotificationService notificationService,
            ICurrencyBotPhasesExecutor currencyBotPhasesExecutor,
            string currency,
            CancellationTokenSource cancellationTokenSource,
            DateTime botStartTime, 
            ICryptoPriceAndRsiQueue<PriceAndRsi> cryptoPriceAndRsiQueue, 
            IAccountQuoteProvider accountQuoteProvider, 
            int age = 0)
        {
            m_currency = currency;
            m_currencyBotFactory = currencyBotFactory;
            m_notificationService = notificationService;
            m_cancellationTokenSource = cancellationTokenSource;
            m_currentTime = botStartTime;
            m_cryptoPriceAndRsiQueue = cryptoPriceAndRsiQueue;
            m_accountQuoteProvider = accountQuoteProvider;
            m_age = age;
            m_currencyBotPhasesExecutor = currencyBotPhasesExecutor;
        }
        
        public async Task<BotResultDetails> StartAsync()
        {
            try
            {
                return await StartAsyncImpl();
            }
            catch (PollingResponseException pollingResponseException)
            {
                return BotResultDetailsFactory.CreateFailureBotResultDetails(pollingResponseException.PollingResponse);
            }
            catch (Exception exception)
            {
                return BotResultDetailsFactory.CreateFailureBotResultDetails(m_currentTime, exception);
            }
        }

        private async Task<BotResultDetails> StartAsyncImpl()
        {
            s_logger.LogInformation($"{m_currency}_{m_age}: Start iteration");

            bool isCandleRed = false;
            while (!isCandleRed)
            {
                m_phasesDescription = new List<string>();
                m_phaseNumber = 0;
                PollingResponseBase pollingResponseBase = await m_currencyBotPhasesExecutor
                    .WaitUntilLowerPriceAndHigherRsiAsync(m_currentTime, m_cancellationTokenSource.Token, m_currency,
                        m_age, ++m_phaseNumber, m_phasesDescription, m_cryptoPriceAndRsiQueue);
                m_currentTime = pollingResponseBase.Time;

                // Validator
                isCandleRed = m_currencyBotPhasesExecutor.ValidateCandleIsRed(m_currentTime, m_currency, m_age,
                    ++m_phaseNumber, m_phasesDescription);
                m_currentTime = await m_currencyBotPhasesExecutor.WaitAsync(m_currentTime,
                    m_cancellationTokenSource.Token, m_currency, s_waitInSecondsAfterValidateReadCandle,
                    "WaitAfterValidateCandleIsRed");
            }

            Task<BotResultDetails> child = StartChildAsync();
            m_currentTime = await m_currencyBotPhasesExecutor.WaitAsync(m_currentTime, 
                m_cancellationTokenSource.Token,
                m_currency,
                s_waitInSecondsBeforeValidateCandleIsGreen, 
                "WaitBeforeValidateCandleIsGreen");
            
            bool isCandleGreen = m_currencyBotPhasesExecutor.ValidateCandleIsGreen(m_currentTime, m_currency, m_age, 
                ++m_phaseNumber, m_phasesDescription);
            if(!isCandleGreen)
            {
                s_logger.LogInformation($"{m_currency}_{m_age}: Done iteration - candle is not green");
                return await child;
            }

            return await OnParentStopRunning();
        }

        private async Task<BotResultDetails> OnParentStopRunning()
        {
            m_cancellationTokenSource.Cancel();
            // Enter
            decimal availableQuote = await m_accountQuoteProvider.GetAvailableQuote();
            BuyAndSellTradeInfo buyAndSellTradeInfo = await m_currencyBotPhasesExecutor.BuyAndPlaceSellOrder(m_currentTime,
                m_currency, m_age, ++m_phaseNumber, m_phasesDescription, availableQuote);
            m_notificationService.Notify($"{string.Join("\n\n", m_phasesDescription)}\n\n" +
                                         $"\tBot buy {m_currency} at price: {buyAndSellTradeInfo.BuyPrice}");
            (bool isWin, PollingResponseBase pollingResponseBase) = 
                await m_currencyBotPhasesExecutor.WaitUnitPriceChangeAsync(m_currentTime, CancellationToken.None, 
                    m_currency, buyAndSellTradeInfo.BuyPrice, m_age, ++m_phaseNumber, m_phasesDescription);
            m_currentTime = pollingResponseBase.Time;
            decimal newQuoteOrderQuantity;
            m_notificationService.Notify(m_phasesDescription.LastOrDefault());
            if (isWin)
            {
                newQuoteOrderQuantity = buyAndSellTradeInfo.QuoteOrderQuantityOnWin;
                s_logger.LogInformation(
                    $"{m_currency}_{m_age}: Done iteration - Win , NewQuoteOrderQuantity: {newQuoteOrderQuantity:f4} {m_currentTime}");
                return BotResultDetailsFactory.CreateSuccessBotResultDetails(BotResult.Win, m_currentTime, m_phasesDescription);
            }

            newQuoteOrderQuantity = buyAndSellTradeInfo.QuoteOrderQuantityOnLoss;
            s_logger.LogInformation(
                $"{m_currency}_{m_age}: Done iteration - Loss, NewQuoteOrderQuantity: {newQuoteOrderQuantity:f4} {m_currentTime}");
            return BotResultDetailsFactory.CreateSuccessBotResultDetails(BotResult.Loss, m_currentTime, m_phasesDescription);
        }

        private async Task<BotResultDetails> StartChildAsync()
        {
            s_logger.LogDebug($"{m_currency}_{m_age}: Start child {m_currentTime}");
            var queue = m_cryptoPriceAndRsiQueue.Clone();
            ICurrencyBot childCurrencyBot = m_currencyBotFactory.Create(queue, 
                m_currency, 
                CancellationTokenSource.CreateLinkedTokenSource(m_cancellationTokenSource.Token),
                m_currentTime, 
                m_age+1);
            return await childCurrencyBot.StartAsync();
        }
    }
}