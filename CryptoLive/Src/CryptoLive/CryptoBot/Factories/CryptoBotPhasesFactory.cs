using Common;
using Common.CryptoQueue;
using CryptoBot.Abstractions;
using CryptoBot.Abstractions.Factories;
using CryptoBot.CryptoPollings;
using CryptoBot.CryptoValidators;
using Storage.Abstractions.Providers;
using Utils.Abstractions;

namespace CryptoBot.Factories
{
    public class CryptoBotPhasesFactory : ICryptoBotPhasesFactory
    {
        public ICurrencyDataProvider CurrencyDataProvider { get; }
        public ISystemClock SystemClock { get; }
        
        public CryptoBotPhasesFactory(ICurrencyDataProvider currencyDataProvider,
            ISystemClock systemClock)
        {
            CurrencyDataProvider = currencyDataProvider;
            SystemClock = systemClock;
        }

        public CryptoPollingBase CreateCandlePolling(decimal basePrice, 
            int delayTimeIterationsInSeconds, 
            int candleSize, decimal 
                priceChangeToNotify)
        {
            decimal minPrice = basePrice * (100 - priceChangeToNotify) / 100;
            decimal maxPrice = basePrice * (100 + priceChangeToNotify) / 100;
            return new CandleCryptoPolling(CurrencyDataProvider, SystemClock, delayTimeIterationsInSeconds, candleSize, minPrice, maxPrice);
        }

        public CryptoPollingBase CreatePriceAndRsiPolling(int candleSize,
            decimal maxRsiToNotify,
            int rsiMemorySize)
        {
            var cryptoPriceAndRsiQueue = new CryptoFixedSizeQueueImpl<PriceAndRsi>(rsiMemorySize);
            return new PriceAndRsiCryptoPolling(CurrencyDataProvider, SystemClock, cryptoPriceAndRsiQueue, maxRsiToNotify);
        }

        public CryptoPollingBase CreateMacdPolling(int macdCandleSize, int maxMacdPollingTime) => 
            new MacdHistogramCryptoPolling(CurrencyDataProvider, SystemClock, maxMacdPollingTime);

        public CryptoPollingBase CreateRsiPolling(decimal maxRsiToNotify) => 
            new RsiCryptoPolling(CurrencyDataProvider, SystemClock, maxRsiToNotify);
        
        
        public RedCandleValidator CreateRedCandleValidator(int candleSize) =>
            new RedCandleValidator(CurrencyDataProvider);

        public GreenCandleValidator CreateGreenCandleValidator(int candleSize) =>
            new GreenCandleValidator(CurrencyDataProvider);

        public MacdHistogramNegativeValidator CreateMacdNegativeValidator(int macdCandleSize) => 
            new MacdHistogramNegativeValidator(CurrencyDataProvider);
    }
}