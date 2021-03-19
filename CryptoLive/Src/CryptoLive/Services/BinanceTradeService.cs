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
        private readonly ICurrencyClientFactory m_currencyClientFactory;

        public BinanceTradeService(ICurrencyClientFactory currencyClientFactory)
        {
            m_currencyClientFactory = currencyClientFactory;
            m_symbolToTickSizesMapping = new Dictionary<string, decimal>();
        }

        public async Task<(decimal buyPrice, decimal quantity)> PlaceBuyMarketOrderAsync(string currency,
            decimal quoteOrderQuantity, DateTime currentTime)
        {
            var action = new Func<Task<WebCallResult<BinancePlacedOrder>>>(async () =>
                await PlaceBuyMarketOrderImplAsync(currency, quoteOrderQuantity));
            var response = await ExecuteTradeAndAssert(action, currency,
                $"Buy Market {quoteOrderQuantity}$");
            return ExtractPriceAndFilledQuantity(response);
        }

        public async Task PlaceSellOcoOrderAsync(string currency, decimal quantity, decimal price,
            decimal stopAndLimitPrice)
        {
            var action = new Func<Task<WebCallResult<BinanceOrderOcoList>>>(async () =>
                await PlaceSellOcoOrderImplAsync(currency, quantity, price, stopAndLimitPrice));
            _ = await ExecuteTradeAndAssert(action, currency,
                $"Sell OCO {quantity}");

        }

        private static async Task<WebCallResult<T>> ExecuteTradeAndAssert<T>(
            Func<Task<WebCallResult<T>>> action, string currency, string description)
        {
            try
            {
                var response = await action.Invoke();
                ResponseHandler.AssertSuccessResponse(response, $"{currency} {description}");
                return response;
            }
            catch (Exception e)
            {
                string message = $"Failed to place {description} for {currency}";
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
        
        private async Task<WebCallResult<BinanceOrderOcoList>> PlaceSellOcoOrderImplAsync(string currency, decimal quantity, decimal price,
            decimal stopAndLimitPrice)
        {
            BinanceClient client = m_currencyClientFactory.Create();
            decimal priceByTickSize = await GetPriceAlignedToTickSizeAsync(currency, price);
            decimal stopAndLimitPriceByTickSize = await GetPriceAlignedToTickSizeAsync(currency, stopAndLimitPrice);
            var response = await client.Spot.Order.PlaceOcoOrderAsync(currency,
                OrderSide.Sell,
                quantity,
                priceByTickSize,
                stopAndLimitPriceByTickSize,
                stopAndLimitPriceByTickSize,
                stopLimitTimeInForce: TimeInForce.GoodTillCancel);
            return response;
        }

        private async ValueTask<decimal> GetPriceAlignedToTickSizeAsync(string currency, decimal price)
        {
            decimal tickSize = await GetTickSizesAsync(currency);
            return (int)(price / tickSize) * tickSize;
        }

        private static (decimal buyPrice, decimal quantity) ExtractPriceAndFilledQuantity(CallResult<BinancePlacedOrder> response)
        {  
            BinanceOrderTrade[] binanceOrderTrades = (response.Data.Fills ?? throw new InvalidOperationException()).ToArray();
            decimal quantityAmount = 0;
            decimal paidAmount = 0;
            foreach (var binanceOrderTrade in binanceOrderTrades)
            {
                quantityAmount += binanceOrderTrade.Quantity;
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
                var response = await client.Spot.System.GetExchangeInfoAsync();
                ResponseHandler.AssertSuccessResponse(response, "GetExchangeInfo");
                BinanceSymbol binanceSymbol = ExtractBinanceSymbol(currency, response);
                tickSizes = ExtractTickSizes(binanceSymbol);
                m_symbolToTickSizesMapping[currency] = tickSizes;
            }

            return tickSizes;
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