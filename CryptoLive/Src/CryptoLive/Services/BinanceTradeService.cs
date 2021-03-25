using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Binance.Net;
using Binance.Net.Enums;
using Binance.Net.Objects.Spot.MarketData;
using Binance.Net.Objects.Spot.SpotData;
using CryptoExchange.Net.Objects;
using Infra;
using Microsoft.Extensions.Logging;
using Services.Abstractions;

namespace Services
{
    public class BinanceTradeService : ITradeService
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<BinanceTradeService>();

        private readonly Dictionary<string, decimal> m_symbolToTickSizesMapping;
        private readonly Dictionary<string, decimal> m_symbolToStepSizesMapping;
        private readonly ICurrencyClientFactory m_currencyClientFactory;

        public BinanceTradeService(ICurrencyClientFactory currencyClientFactory)
        {
            m_currencyClientFactory = currencyClientFactory;
            m_symbolToTickSizesMapping = new Dictionary<string, decimal>();
            m_symbolToStepSizesMapping = new Dictionary<string, decimal>();
        }

        public async Task<(decimal buyPrice, decimal quantity)> PlaceBuyMarketOrderAsync(string currency,
            decimal quoteOrderQuantity, DateTime currentTime)
        {
            decimal quoteOrderQuantityByTickSize = await GetPriceAlignedToTickSizeAsync(currency, quoteOrderQuantity);
            var action = new Func<Task<WebCallResult<BinancePlacedOrder>>>(async () =>
                await PlaceBuyMarketOrderImplAsync(currency, quoteOrderQuantityByTickSize));
            string actionDescription = $"Place Market Buy {quoteOrderQuantityByTickSize}$ of {currency}";
            var response = await ExecuteTradeAndAssert(action, actionDescription);
            (decimal buyPrice, decimal quantity) = ExtractPriceAndFilledQuantity(response);
            s_logger.LogDebug($"{currency}: buy Price: {buyPrice}, quantity: {quantity}");
            return (buyPrice, quantity);
        }

        public async Task PlaceSellOcoOrderAsync(string currency, decimal quantity, decimal price,
            decimal stopAndLimitPrice)
        {
            decimal priceByTickSize = await GetPriceAlignedToTickSizeAsync(currency, price);
            decimal stopAndLimitPriceByTickSize = await GetPriceAlignedToTickSizeAsync(currency, stopAndLimitPrice);
            decimal quantityByStepSizes = await GetQuantityAlignedToStepSizeAsync(currency, quantity);
            var action = new Func<Task<WebCallResult<BinanceOrderOcoList>>>(async () =>
                await PlaceSellOcoOrderImplAsync(currency, quantityByStepSizes, priceByTickSize, stopAndLimitPriceByTickSize));
            string actionDescription = $"place OCO sell {quantityByStepSizes} {currency}, " +
                                       $"price {priceByTickSize}, StopAndLimit: {stopAndLimitPriceByTickSize}";
            _ = await ExecuteTradeAndAssert(action, actionDescription);
        }

        private static async Task<WebCallResult<T>> ExecuteTradeAndAssert<T>(
            Func<Task<WebCallResult<T>>> action, string actionDescription)
        {
            try
            {
                s_logger.LogDebug($"Start {actionDescription}");
                var response = await HttpRequestRetryHandler.RetryOnFailure(
                    async () => await action.Invoke(),
                    actionDescription);
                s_logger.LogDebug($"Success {actionDescription}");
                return response;
            }
            catch (Exception e)
            {
                string message = $"Failed {actionDescription}";
                s_logger.LogError(e, message);
                throw new Exception(message);
            }
        }

        private async Task<WebCallResult<BinancePlacedOrder>> PlaceBuyMarketOrderImplAsync(string currency, decimal quoteOrderQuantity)
        {
            BinanceClient client = m_currencyClientFactory.Create();
            var response = await client.Spot.Order.PlaceOrderAsync(currency, OrderSide.Buy, OrderType.Market,
                quoteOrderQuantity: quoteOrderQuantity);
            return response;
        }
        
        private async Task<WebCallResult<BinanceOrderOcoList>> PlaceSellOcoOrderImplAsync(string currency, 
            decimal quantity, 
            decimal price,
            decimal stopAndLimitPrice)
        {
            BinanceClient client = m_currencyClientFactory.Create();
            var response = await client.Spot.Order.PlaceOcoOrderAsync(currency,
                OrderSide.Sell,
                quantity,
                price,
                stopAndLimitPrice,
                stopAndLimitPrice,
                stopLimitTimeInForce: TimeInForce.GoodTillCancel);
            return response;
        }

        private async ValueTask<decimal> GetPriceAlignedToTickSizeAsync(string currency, decimal price)
        {
            decimal tickSize = await GetTickSizesAsync(currency);
            return (int)(price / tickSize) * tickSize;
        }
        
        private async ValueTask<decimal> GetQuantityAlignedToStepSizeAsync(string currency, decimal quantity)
        {
            decimal stepSizes = await GetStepSizesAsync(currency);
            return (int)(quantity / stepSizes) * stepSizes;
        }

        private static (decimal buyPrice, decimal quantity) ExtractPriceAndFilledQuantity(CallResult<BinancePlacedOrder> response)
        {  
            BinanceOrderTrade[] binanceOrderTrades = (response.Data.Fills ?? throw new InvalidOperationException()).ToArray();
            decimal quantityAmount = 0;
            decimal paidAmount = 0;
            foreach (var binanceOrderTrade in binanceOrderTrades)
            {
                quantityAmount = quantityAmount + binanceOrderTrade.Quantity - binanceOrderTrade.Commission;
                paidAmount += binanceOrderTrade.Price * binanceOrderTrade.Quantity;
            }

            decimal avgBuyPrice = paidAmount / quantityAmount;
            return (avgBuyPrice, quantityAmount);
        }

        private async ValueTask<decimal> GetTickSizesAsync(string currency)
        {
            if (!m_symbolToTickSizesMapping.TryGetValue(currency, out decimal tickSizes))
            {
                BinanceClient client = m_currencyClientFactory.Create();
                var response = await HttpRequestRetryHandler.RetryOnFailure(
                    async () =>  await client.Spot.System.GetExchangeInfoAsync(),
                    "GetExchangeInfo");
                BinanceSymbol binanceSymbol = ExtractBinanceSymbol(currency, response);
                tickSizes = ExtractTickSizes(binanceSymbol);
                m_symbolToTickSizesMapping[currency] = tickSizes;
            }

            return tickSizes;
        }
        
        private async ValueTask<decimal> GetStepSizesAsync(string currency)
        {
            if (!m_symbolToStepSizesMapping.TryGetValue(currency, out decimal stepSizes))
            {
                BinanceClient client = m_currencyClientFactory.Create();
                var response = await HttpRequestRetryHandler.RetryOnFailure(
                    async () =>  await client.Spot.System.GetExchangeInfoAsync(),
                    "GetExchangeInfo");
                BinanceSymbol binanceSymbol = ExtractBinanceSymbol(currency, response);
                stepSizes = ExtractStepSizes(binanceSymbol);
                m_symbolToStepSizesMapping[currency] = stepSizes;
            }

            return stepSizes;
        }

        private static decimal ExtractTickSizes(BinanceSymbol binanceSymbol)
        {
            BinanceSymbolFilter priceFilter = binanceSymbol.Filters.SingleOrDefault(m => m.FilterType== SymbolFilterType.Price);
            if (priceFilter is null)
            {
                throw new Exception($"{binanceSymbol.Name} exchange info doesn't include PriceFilter");
            }

            return ((BinanceSymbolPriceFilter) priceFilter).TickSize;
        }
        
        private static decimal ExtractStepSizes(BinanceSymbol binanceSymbol)
        {
            BinanceSymbolFilter lotSizeFilter = binanceSymbol.Filters.SingleOrDefault(m => m.FilterType== SymbolFilterType.LotSize);
            if (lotSizeFilter is null)
            {
                throw new Exception($"{binanceSymbol.Name} exchange info doesn't include LotSizeFilter");
            }

            return ((BinanceSymbolLotSizeFilter) lotSizeFilter).StepSize;
        }

        private static BinanceSymbol ExtractBinanceSymbol(string currency, CallResult<BinanceExchangeInfo> response)
        {
            var binanceSymbol = response.Data.Symbols.SingleOrDefault(m => m.Name.Equals(currency));
            if (binanceSymbol is null)
            {
                throw new Exception($"{currency} exchange info not found");
            }
            return binanceSymbol;
        }
    }
}