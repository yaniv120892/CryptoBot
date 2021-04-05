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

        public async Task<long> PlaceBuyLimitOrderAsync(string currency,
            decimal limitPrice,
            decimal quantity, 
            DateTime currentTime)
        {
            decimal priceByTickSize = await GetPriceAlignedToTickSizeAsync(currency, limitPrice);
            decimal quantityByStepSize = await GetQuantityAlignedToStepSizeAsync(currency, quantity);

            var action = new Func<Task<WebCallResult<BinancePlacedOrder>>>(async () =>
                await PlaceBuyLimitOrderImplAsync(currency, quantityByStepSize, priceByTickSize));
            string actionDescription = $"Place Limit Buy {quantityByStepSize}$ of {currency}";
            var response = await ExecuteTradeAndAssert(action, actionDescription);
            long orderId = ExtractOrderId(response);
            s_logger.LogDebug($"{currency}: buy Price: {priceByTickSize}, quantity: {quantityByStepSize}");
            return orderId;
        }

        public async Task CancelOrderAsync(string currency, long orderId)
        {
            var action = new Func<Task<WebCallResult<BinanceCanceledOrder>>>(async () =>
                await CancelOrderImplAsync(currency, orderId));
            string actionDescription = $"Cancel order {orderId}";
            await ExecuteTradeAndAssert(action, actionDescription);
            s_logger.LogDebug($"{currency}: Cancel order: {orderId}");
        }
        
        public async Task<string> GetOrderStatusAsync(string currency, long orderId, DateTime currentTime)
        {
            var action = new Func<Task<WebCallResult<BinanceOrder>>>(async () =>
                await GetOrderStatusImplAsync(currency, orderId));
            string actionDescription = $"Get status for order {orderId}";
            var response = await ExecuteTradeAndAssert(action, actionDescription);
            string orderStatus = ExtractOrderStatusFromResponse(response);
            s_logger.LogDebug($"{currency}: order {orderId} status: {orderStatus}");
            return orderStatus;
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

        private async Task<WebCallResult<BinancePlacedOrder>> PlaceBuyLimitOrderImplAsync(string currency, decimal quantity, decimal limitPrice)
        {
            BinanceClient client = m_currencyClientFactory.Create();
            var response = await client.Spot.Order.PlaceOrderAsync(currency, OrderSide.Buy, OrderType.Limit,
                quantity: quantity, price:limitPrice, timeInForce:TimeInForce.GoodTillCancel, orderResponseType:OrderResponseType.Acknowledge);
            return response;
        }
        
        private async Task<WebCallResult<BinanceCanceledOrder>> CancelOrderImplAsync(string currency, long orderId)
        {
            BinanceClient client = m_currencyClientFactory.Create();
            var response = await client.Spot.Order.CancelOrderAsync(currency, orderId);
            return response;        
        }
        
        private async Task<WebCallResult<BinanceOrder>> GetOrderStatusImplAsync(string currency, long orderId)
        {
            BinanceClient client = m_currencyClientFactory.Create();
            var response = await client.Spot.Order.GetOrderAsync(currency, orderId);
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

        private static long ExtractOrderId(WebCallResult<BinancePlacedOrder> response) => 
            response.Data.OrderId;

        private static string ExtractOrderStatusFromResponse(WebCallResult<BinanceOrder> response) => 
            response.Data.Status.ToString();
        
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