using System.Collections.Generic;
using System.Threading;
using Common;
using Common.Abstractions;
using CryptoBot.Abstractions;
using CryptoBot.Abstractions.Factories;
using CryptoBot.CryptoPollings;
using CryptoBot.CryptoTraders;
using CryptoBot.CryptoValidators;
using Services.Abstractions;
using Storage.Abstractions.Providers;
using Utils.Abstractions;

namespace CryptoBot.Factories
{
    public class CryptoBotPhasesFactory : ICryptoBotPhasesFactory
    {
        private readonly ITradeService m_tradeService;
        public ICurrencyDataProvider CurrencyDataProvider { get; }
        public ISystemClock SystemClock { get; }
        
        public CryptoBotPhasesFactory(ICurrencyDataProvider currencyDataProvider,
            ISystemClock systemClock, 
            ITradeService tradeService)
        {
            CurrencyDataProvider = currencyDataProvider;
            SystemClock = systemClock;
            m_tradeService = tradeService;
        }

        public ICryptoPolling CreateCandlePolling(decimal minPrice, decimal maxPrice, int candleSize) =>
            new CandleCryptoPolling(CurrencyDataProvider, SystemClock, candleSize, minPrice, maxPrice);

        public ICryptoPolling CreatePriceAndRsiPolling(decimal maxRsiToNotify,
            ICryptoPriceAndRsiQueue<PriceAndRsi> cryptoPriceAndRsiQueue,
            Queue<CancellationToken> parentRunningCancellationToken,
            int iterationToRunBeforeWaitingForParentToFinish) =>
            new PriceAndRsiCryptoPolling(CurrencyDataProvider, 
                SystemClock, 
                cryptoPriceAndRsiQueue, 
                maxRsiToNotify, 
                parentRunningCancellationToken,
                iterationToRunBeforeWaitingForParentToFinish);
        
        public ICryptoPolling CreateOrderStatusPolling(long orderId) =>
            new OrderCryptoPolling(SystemClock, m_tradeService, orderId);

        public RedCandleValidator CreateRedCandleValidator() =>
            new RedCandleValidator(CurrencyDataProvider);

        public GreenCandleValidator CreateGreenCandleValidator() =>
            new GreenCandleValidator(CurrencyDataProvider);

        public MeanAveragePriceValidator CreateMeanAveragePriceValidator() =>
            new MeanAveragePriceValidator(CurrencyDataProvider);

        public IBuyCryptoTrader CreateStopLimitBuyCryptoTrader() => new LimitBuyCryptoTrader(m_tradeService);
        public ISellCryptoTrader CreateOcoSellCryptoTrader() => new OcoSellCryptoTrader(m_tradeService);
        public ICancelOrderCryptoTrader CreateCancelOrderCryptoTrader() => new CancelOrderCryptoTrader(m_tradeService);
    }
}