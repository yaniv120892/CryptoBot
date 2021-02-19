using Common;
using Common.CryptoQueue;
using CryptoBot.Abstractions.Factories;
using CryptoBot.CryptoPollings;
using CryptoBot.CryptoValidators;
using Infra;
using Storage.Abstractions.Providers;
using Utils.Abstractions;

namespace CryptoBot.Factories
{
    public class CryptoBotPhasesFactory : ICryptoBotPhasesFactory
    {
        private readonly INotificationService m_notificationService;
        public ICurrencyDataProvider CurrencyDataProvider { get; }
        public ISystemClock SystemClock { get; }
        
        public CryptoBotPhasesFactory(ICurrencyDataProvider currencyDataProvider,
            ISystemClock systemClock, 
            INotificationService notificationService)
        {
            CurrencyDataProvider = currencyDataProvider;
            SystemClock = systemClock;
            m_notificationService = notificationService;
        }

        public CandleCryptoPolling CreateCandlePolling(decimal basePrice, 
            int delayTimeIterationsInSeconds, 
            int candleSize, decimal 
                priceChangeToNotify)
        {
            decimal minPrice = basePrice * (100 - priceChangeToNotify) / 100;
            decimal maxPrice = basePrice * (100 + priceChangeToNotify) / 100;
            return new CandleCryptoPolling(m_notificationService, CurrencyDataProvider, SystemClock, delayTimeIterationsInSeconds, candleSize, minPrice, maxPrice);
        }

        public PriceAndRsiCryptoPolling CreatePriceAndRsiPolling(int candleSize,
            decimal maxRsiToNotify,
            int rsiMemorySize)
        {
            var cryptoPriceAndRsiQueue = new CryptoFixedSizeQueueImpl<PriceAndRsi>(rsiMemorySize);
            return new PriceAndRsiCryptoPolling(m_notificationService, CurrencyDataProvider, SystemClock, cryptoPriceAndRsiQueue, maxRsiToNotify);
        }

        public MacdHistogramCryptoPolling CreateMacdPolling(int macdCandleSize, int maxMacdPollingTime) => 
            new MacdHistogramCryptoPolling(null, CurrencyDataProvider, SystemClock, maxMacdPollingTime);

        public RsiCryptoPolling CreateRsiPolling(decimal maxRsiToNotify) => 
            new RsiCryptoPolling(m_notificationService, CurrencyDataProvider, SystemClock, maxRsiToNotify);
        
        
        public RedCandleValidator CreateRedCandleValidator(int candleSize) =>
            new RedCandleValidator(m_notificationService, CurrencyDataProvider);

        public GreenCandleValidator CreateGreenCandleValidator(int candleSize) =>
            new GreenCandleValidator(m_notificationService, CurrencyDataProvider);

        public MacdHistogramNegativeValidator CreateMacdNegativeValidator(int macdCandleSize) => 
            new MacdHistogramNegativeValidator(m_notificationService, CurrencyDataProvider);
    }
}