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
        private readonly CancellationTokenSource m_isRunningCancellationTokenSource;
        private readonly int m_age;
        private readonly string m_currency;
        private readonly Queue<CancellationToken> m_parentRunningCancellationToken;

        private Task<BotResultDetails> m_child;
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
            Queue<CancellationToken> parentRunningCancellationToken, 
            int age = 0)
        {
            m_currency = currency;
            m_currencyBotFactory = currencyBotFactory;
            m_notificationService = notificationService;
            m_cancellationTokenSource = cancellationTokenSource;
            m_currentTime = botStartTime;
            m_cryptoPriceAndRsiQueue = cryptoPriceAndRsiQueue;
            m_accountQuoteProvider = accountQuoteProvider;
            m_parentRunningCancellationToken = parentRunningCancellationToken;
            m_age = age;
            m_currencyBotPhasesExecutor = currencyBotPhasesExecutor;
            m_isRunningCancellationTokenSource = new CancellationTokenSource();
        }
        
        public async Task<BotResultDetails> StartAsync()
        {
            BotResultDetails botResultDetails;
            try
            {
                botResultDetails = await StartAsyncImpl();
            }
            catch (PollingResponseException pollingResponseException)
            {
                botResultDetails = BotResultDetailsFactory.CreateFailureBotResultDetails(pollingResponseException.PollingResponse);
            }
            catch (Exception exception)
            {
                botResultDetails = BotResultDetailsFactory.CreateFailureBotResultDetails(m_currentTime, exception);
            }
            finally
            {
                StopRunningChildren();
            }

            return botResultDetails;
        }

        private async Task<BotResultDetails> StartAsyncImpl()
        {
            s_logger.LogInformation($"{m_currency}_{m_age}: Start iteration");

            if (await WaitForSignal())
            {
                StopRunningChildren();
                return await OnFoundSignal();
            }

            ReleaseWaitingChildren();
            return await m_child;
        }

        private async Task<bool> WaitForSignal()
        {
            bool isCandleRed = false;
            while (!isCandleRed)
            {
                m_phasesDescription = new List<string>();
                m_phaseNumber = 0;
                PollingResponseBase pollingResponseBase = await m_currencyBotPhasesExecutor
                    .WaitUntilLowerPriceAndHigherRsiAsync(m_currentTime, m_cancellationTokenSource.Token, m_currency,
                        m_age, ++m_phaseNumber, m_phasesDescription, m_cryptoPriceAndRsiQueue,
                        m_parentRunningCancellationToken);
                m_currentTime = pollingResponseBase.Time;

                // Validator
                isCandleRed = m_currencyBotPhasesExecutor.ValidateCandleIsRed(m_currentTime, m_currency, m_age,
                    ++m_phaseNumber, m_phasesDescription);
                m_currentTime = await m_currencyBotPhasesExecutor.WaitAsync(m_currentTime,
                    m_cancellationTokenSource.Token, m_currency, s_waitInSecondsAfterValidateReadCandle,
                    "WaitAfterValidateCandleIsRed");
            }

            m_child = StartChildAsync();
            m_currentTime = await m_currencyBotPhasesExecutor.WaitAsync(m_currentTime,
                m_cancellationTokenSource.Token,
                m_currency,
                s_waitInSecondsBeforeValidateCandleIsGreen,
                "WaitBeforeValidateCandleIsGreen");

            bool isCandleGreen = m_currencyBotPhasesExecutor.ValidateCandleIsGreen(m_currentTime, m_currency, m_age,
                ++m_phaseNumber, m_phasesDescription);
            s_logger.LogInformation($"{m_currency}_{m_age}: Done iteration - candle is green {isCandleGreen}");

            return isCandleGreen;
        }

        private void ReleaseWaitingChildren()
        {
            m_isRunningCancellationTokenSource.Cancel();
        }

        private async Task<BotResultDetails> OnFoundSignal()
        {
            // Enter
            var buyAndSellTradeInfo = await PlaceBuyAndSellOrder();
            var isWin = await WaitForOpenOrdersToBeFilled(buyAndSellTradeInfo);
            return GetBotResultDetails(isWin, buyAndSellTradeInfo);
        }

        private BotResultDetails GetBotResultDetails(bool isWin, BuyAndSellTradeInfo buyAndSellTradeInfo)
        {
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

        private async Task<bool> WaitForOpenOrdersToBeFilled(BuyAndSellTradeInfo buyAndSellTradeInfo)
        {
            (bool isWin, PollingResponseBase pollingResponseBase) =
                await m_currencyBotPhasesExecutor.WaitUnitPriceChangeAsync(m_currentTime, CancellationToken.None,
                    m_currency, buyAndSellTradeInfo.BuyPrice, m_age, ++m_phaseNumber, m_phasesDescription);
            m_currentTime = pollingResponseBase.Time;
            return isWin;
        }

        private async Task<BuyAndSellTradeInfo> PlaceBuyAndSellOrder()
        {
            decimal availableQuote = await m_accountQuoteProvider.GetAvailableQuote();
            BuyAndSellTradeInfo buyAndSellTradeInfo = await m_currencyBotPhasesExecutor.BuyAndPlaceSellOrder(m_currentTime,
                m_currency, m_age, ++m_phaseNumber, m_phasesDescription, availableQuote);
            m_notificationService.Notify($"{string.Join("\n\n", m_phasesDescription)}\n\n" +
                                         $"\tBot buy {m_currency} at price: {buyAndSellTradeInfo.BuyPrice}");
            return buyAndSellTradeInfo;
        }

        private void StopRunningChildren()
        {
            m_cancellationTokenSource.Cancel();
        }

        private async Task<BotResultDetails> StartChildAsync()
        {
            s_logger.LogDebug($"{m_currency}_{m_age}: Start child {m_currentTime}");
            var cryptoPriceAndRsiQueue = m_cryptoPriceAndRsiQueue.Clone();
            var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(m_cancellationTokenSource.Token);
            m_parentRunningCancellationToken.Enqueue(m_isRunningCancellationTokenSource.Token);
            int childAge = m_age + 1;
            
            ICurrencyBot childCurrencyBot = m_currencyBotFactory.Create(cryptoPriceAndRsiQueue,
                m_parentRunningCancellationToken,
                m_currency, 
                linkedCancellationTokenSource,
                m_currentTime, 
                childAge);
            return await childCurrencyBot.StartAsync();
        }
    }
}