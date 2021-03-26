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

        public CryptoPollingBase CreateCandlePolling(decimal basePrice, 
            int delayTimeIterationsInSeconds, 
            int candleSize, 
            decimal priceChangeToNotify)
        {
            decimal minPrice = basePrice * (100 - priceChangeToNotify) / 100;
            decimal maxPrice = basePrice * (100 + priceChangeToNotify) / 100;
            return new CandleCryptoPolling(CurrencyDataProvider, SystemClock, delayTimeIterationsInSeconds, candleSize, minPrice, maxPrice);
        }
        
        public CryptoPollingBase CreatePriceAndRsiPolling(decimal maxRsiToNotify,
            ICryptoPriceAndRsiQueue<PriceAndRsi> cryptoPriceAndRsiQueue,
            Queue<CancellationToken> parentRunningCancellationToken,
            int iterationToRunBeforeWaitingForParentToFinish) =>
            new PriceAndRsiCryptoPolling(CurrencyDataProvider, 
                SystemClock, 
                cryptoPriceAndRsiQueue, 
                maxRsiToNotify, 
                parentRunningCancellationToken,
                iterationToRunBeforeWaitingForParentToFinish);

        public RedCandleValidator CreateRedCandleValidator(int candleSize) =>
            new RedCandleValidator(CurrencyDataProvider);

        public GreenCandleValidator CreateGreenCandleValidator(int candleSize) =>
            new GreenCandleValidator(CurrencyDataProvider);

        public IBuyCryptoTrader CreateMarketBuyCryptoTrader() => new MarketBuyCryptoTrader(m_tradeService);
        public ISellCryptoTrader CreateOcoSellCryptoTrader() => new OcoSellCryptoTrader(m_tradeService);
    }
}