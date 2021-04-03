#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Abstractions;
using Common.PollingResponses;
using CryptoBot.Abstractions;
using CryptoBot.Abstractions.Factories;
using CryptoBot.CryptoValidators;
using CryptoBot.Exceptions;
using Infra;
using Microsoft.Extensions.Logging;

namespace CryptoBot
{
    public class CurrencyBotPhasesExecutor : ICurrencyBotPhasesExecutor
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<CurrencyBotPhasesExecutor>();
        private static readonly decimal s_transactionFee = new decimal(0.2);
        private static readonly int s_priceChangeCandleSize = 1;

        private readonly ICryptoBotPhasesFactory m_cryptoBotPhasesFactory;
        private readonly int m_redCandleSize;
        private readonly int m_greenCandleSize;
        private readonly decimal m_maxRsiToNotify;
        private readonly decimal m_priceChangeToNotify;

        public CurrencyBotPhasesExecutor(ICryptoBotPhasesFactory cryptoBotPhasesFactory, 
            decimal maxRsiToNotify, 
            int redCandleSize, 
            int greenCandleSize, 
            decimal priceChangeToNotify)
        {
            m_cryptoBotPhasesFactory = cryptoBotPhasesFactory;
            m_maxRsiToNotify = maxRsiToNotify;
            m_redCandleSize = redCandleSize;
            m_greenCandleSize = greenCandleSize;
            m_priceChangeToNotify = priceChangeToNotify;
        }

        public async Task<PollingResponseBase> WaitUntilLowerPriceAndHigherRsiAsync(DateTime currentTime, 
            CancellationToken cancellationToken, 
            string currency,
            int age, 
            int phaseNumber, 
            List<string> phasesDescription, 
            ICryptoPriceAndRsiQueue<PriceAndRsi> cryptoPriceAndRsiQueue,
            Queue<CancellationToken> parentRunningCancellationToken)
        {
            s_logger.LogInformation(
                $"{currency}_{age}: Start phase {phaseNumber}: wait until lower price and higher RSI is {currentTime:dd/MM/yyyy HH:mm:ss}");
            ICryptoPolling priceAndRsiPolling = m_cryptoBotPhasesFactory
                .CreatePriceAndRsiPolling(m_maxRsiToNotify, cryptoPriceAndRsiQueue, parentRunningCancellationToken, 
                    m_greenCandleSize-parentRunningCancellationToken.Count);
            PollingResponseBase responseBase =
                await priceAndRsiPolling.StartAsync(currency, cancellationToken, currentTime);
            AssertSuccessPolling(responseBase);
            s_logger.LogInformation(
                $"{currency}_{age}: Done phase {phaseNumber}: wait until lower price and higher RSI {responseBase.Time:dd/MM/yyyy HH:mm:ss}");
            phasesDescription.Add(
                $"{currency} {responseBase.Time:dd/MM/yyyy HH:mm:ss}\n{phaseNumber}.Found candle with lower price and higher RSI, \n" +
                $"\tCurrent candle: {((PriceAndRsiPollingResponse) responseBase).NewPriceAndRsi}, \n" +
                $"\tOld candle: {((PriceAndRsiPollingResponse) responseBase).OldPriceAndRsi}\n");

            return responseBase;        
        }

        public async Task<(bool, PollingResponseBase)> WaitUnitPriceChangeAsync(DateTime currentTime,
            CancellationToken cancellationToken,
            string currency,
            decimal minPrice,
            decimal maxPrice,
            int age,
            int phaseNumber,
            List<string> phasesDescription)
        {
            s_logger.LogInformation($"{currency}_{age} Start phase {phaseNumber}: get price every {s_priceChangeCandleSize} minutes until break {minPrice}-{maxPrice}, {currentTime:dd/MM/yyyy HH:mm:ss}");
            currentTime = await m_cryptoBotPhasesFactory.SystemClock.Wait(cancellationToken, currency,s_priceChangeCandleSize * 60, "FullMode_WaitBeforeStartPricePolling", currentTime);
            ICryptoPolling candlePolling = m_cryptoBotPhasesFactory.CreateCandlePolling(minPrice, maxPrice, s_priceChangeCandleSize);
            PollingResponseBase responseBase = await candlePolling.StartAsync(currency,cancellationToken, currentTime);
            AssertSuccessPolling(responseBase);
            var candlePollingResponse = AssertIsCandlePollingResponse(responseBase);
            string increaseOrDecreaseStr = candlePollingResponse.IsWin ? "increase by" : "decreased by";
            s_logger.LogInformation($"{currency}_{age} Done phase {phaseNumber}: price {increaseOrDecreaseStr} " +
                                    $"{m_priceChangeToNotify}%, " + $"{candlePollingResponse.Time:dd/MM/yyyy HH:mm:ss}");
            phasesDescription.Add($"{currency} {candlePollingResponse.Time:dd/MM/yyyy HH:mm:ss}\n{(candlePollingResponse.IsWin ? "WIN": "LOSS")}\n");
            return (candlePollingResponse.IsWin, candlePollingResponse);
        }

        public bool ValidateCandleIsRed(DateTime currentTime,
            string currency,
            int age,
            int phaseNumber, 
            List<string> phasesDescription)
        {
            s_logger.LogInformation($"{currency}_{age} Start phase {phaseNumber}: validate current candle is red {currentTime:dd/MM/yyyy HH:mm:ss}");
            RedCandleValidator redCandleValidator = m_cryptoBotPhasesFactory.CreateRedCandleValidator();
            bool isCandleRed = redCandleValidator.Validate(currency, m_redCandleSize, currentTime);
            s_logger.LogInformation($"{currency}_{age} Done phase {phaseNumber}: current candle is red: {isCandleRed} {currentTime:dd/MM/yyyy HH:mm:ss}");
            phasesDescription.Add($"{currency} {currentTime:dd/MM/yyyy HH:mm:ss}\n{phaseNumber}.Validate candle is Red: {isCandleRed}\n");
            return isCandleRed;        
        }

        public bool ValidateCandleIsGreen(DateTime currentTime,
            string currency,
            int age,
            int phaseNumber, List<string> phasesDescription)
        {
            s_logger.LogInformation($"{currency}_{age} Start phase {phaseNumber}: validate current candle is green {currentTime:dd/MM/yyyy HH:mm:ss}");
            GreenCandleValidator greenCandleValidator = m_cryptoBotPhasesFactory.CreateGreenCandleValidator();
            bool isCandleGreen = greenCandleValidator.Validate(currency, m_greenCandleSize, currentTime);
            s_logger.LogInformation($"{currency}_{age} Done phase {phaseNumber}: current candle is green: {isCandleGreen} {currentTime:dd/MM/yyyy HH:mm:ss}");
            phasesDescription.Add($"{currency} {currentTime:dd/MM/yyyy HH:mm:ss}\n{phaseNumber}.Validate candle is green: {isCandleGreen}\n");
            return isCandleGreen;
        }

        public Task<DateTime> WaitAsync(DateTime currentTime,
            CancellationToken cancellationToken,
            string currency, 
            int timeToWaitInSeconds,
            string action) =>
            m_cryptoBotPhasesFactory.SystemClock.Wait(cancellationToken,currency,timeToWaitInSeconds, action, currentTime);

        public Task<DateTime> WaitForNextCandleAsync(DateTime currentTime, CancellationToken token, string currency) => 
            WaitAsync(currentTime, token, currency, (m_greenCandleSize-1) * 60, "WaitBeforeValidateCandleIsGreen");

        public async Task<BuyAndSellTradeInfo> BuyAndPlaceSellOrder(DateTime currentTime,
            CancellationToken cancellationToken,
            string currency,
            int age,
            int phaseNumber,
            List<string> phasesDescription,
            decimal buyPrice,
            decimal quoteOrderQuantity)
        {
            s_logger.LogInformation($"{currency}_{age} Start phase {phaseNumber}: Buy coin and place sell order {currentTime:dd/MM/yyyy HH:mm:ss}");
            DateTime startTradeTime = currentTime;
            IBuyCryptoTrader buyCryptoTrader = m_cryptoBotPhasesFactory.CreateStopLimitBuyCryptoTrader();
            decimal quantity = quoteOrderQuantity / buyPrice;
            long orderId = await buyCryptoTrader.BuyAsync(currency, buyPrice, quantity, currentTime);
            currentTime = await WaitAsync(currentTime, cancellationToken, currency, 60, "WaitBeforeStartPollingOrderStatus");
            ICryptoPolling orderCryptoPolling = m_cryptoBotPhasesFactory.CreateOrderStatusPolling(orderId);
            PollingResponseBase pollingResponseBase = await orderCryptoPolling.StartAsync(currency, cancellationToken, currentTime);
            if (!pollingResponseBase.IsSuccess)
            {
                await CancelBuyLimitOrder(orderId, currency);
                phasesDescription.Add(
                    $"{currency} {currentTime:dd/MM/yyyy HH:mm:ss}\n{phaseNumber}.Place Buy limit order at {buyPrice} expired");
                return new BuyAndSellTradeInfo(buyPrice,quantity, startTradeTime, pollingResponseBase.Time);
            }

            ISellCryptoTrader sellCryptoTrader = m_cryptoBotPhasesFactory.CreateOcoSellCryptoTrader();
            decimal sellPrice = buyPrice * (100 + m_priceChangeToNotify + s_transactionFee) / 100;
            decimal stopAndLimitPrice = buyPrice * (100 - m_priceChangeToNotify + s_transactionFee) / 100;
            await sellCryptoTrader.SellAsync(currency, quantity, sellPrice, stopAndLimitPrice);
            s_logger.LogInformation($"{currency}_{age} Done phase {phaseNumber}: Bought {quantity:F6} {currency.Replace("USDT",String.Empty)} at price {buyPrice}, " +
                                    $"place sell order for {sellPrice} and stop loss limit {stopAndLimitPrice} {currentTime:dd/MM/yyyy HH:mm:ss}");
            var buyAndSellTradeInfo = new BuyAndSellTradeInfo(buyPrice, sellPrice, stopAndLimitPrice, quantity, startTradeTime, pollingResponseBase.Time);
            string currencyWithoutUsdt = currency.Replace("USDT", String.Empty);
            phasesDescription.Add($"{currency} {currentTime:dd/MM/yyyy HH:mm:ss}\n{phaseNumber}.Buy {currencyWithoutUsdt}\n" +
                                  $"Amount:{quantity:F8}\n" +
                                  $"Price:{buyPrice:F8}\n" +
                                  $"Sell Price: {sellPrice:F8}\n" +
                                  $"Stop Loss: {stopAndLimitPrice:F8}\n" +
                                  $"Total Paid: {buyAndSellTradeInfo.QuoteOrderQuantityPaid:F2}\n" +
                                  $"Total on WIN: {buyAndSellTradeInfo.QuoteOrderQuantityOnWin:F2}\n" +
                                  $"Total on LOSS: {buyAndSellTradeInfo.QuoteOrderQuantityOnLoss:F2}");
            return buyAndSellTradeInfo;
        }

        public decimal GetLastRecordedPrice(string currency, DateTime currentTime) => 
            m_cryptoBotPhasesFactory.CurrencyDataProvider.GetPriceAsync(currency, currentTime);

        private async Task CancelBuyLimitOrder(long orderId, string currency)
        {
            ICancelOrderCryptoTrader cancelOrderCryptoTrader = m_cryptoBotPhasesFactory.CreateCancelOrderCryptoTrader();
            await cancelOrderCryptoTrader.CancelAsync(currency, orderId);
        }

        private static CandlePollingResponse AssertIsCandlePollingResponse(PollingResponseBase pollingResponseBase)
        {
            if (!(pollingResponseBase is CandlePollingResponse candlePollingResponse))
            {
                throw new Exception("candle polling response should be of type CandlePollingResponse");
            }

            return candlePollingResponse;
        }

        private static void AssertSuccessPolling(PollingResponseBase pollingResponseBase)
        {
            if (!pollingResponseBase.IsSuccess)
            {
                throw new PollingResponseException(pollingResponseBase);
            }
        }
    }
}