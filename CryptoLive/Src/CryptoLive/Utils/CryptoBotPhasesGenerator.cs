using Infra;
using Utils.Abstractions;
using Utils.CryptoPollings;
using Utils.CryptoValidators;
using Utils.NotificationHandlers;

namespace Utils
{
    public class CryptoBotPhasesFactory : ICryptoBotPhasesFactory
    {
        private readonly INotificationService m_notificationService;
        public ICurrencyService CurrencyService { get; }
        public ISystemClock SystemClock { get; }
        
        public CryptoBotPhasesFactory(INotificationService notificationService, ICurrencyService currencyService, ISystemClock systemClock)
        {
            m_notificationService = notificationService;
            CurrencyService = currencyService;
            SystemClock = systemClock;
        }
        
        public CandlePolling CreateCandlePolling(decimal basePrice, int delayTimeIterationsInSeconds, int candleSize, decimal priceChangeToNotify)
        {
            INotificationHandler notificationHandler = new ChangePriceNotificationHandler(m_notificationService, basePrice, priceChangeToNotify);
            return new CandlePolling(notificationHandler, CurrencyService, SystemClock, delayTimeIterationsInSeconds, candleSize);
        }

        public RsiPolling CreateRsiPolling(int candleSize, decimal maxRsiToNotify, int candlesAmount)
        {
            INotificationHandler notificationHandler = new RsiDropNotificationHandler(m_notificationService, maxRsiToNotify);
            return new RsiPolling(notificationHandler, CurrencyService, SystemClock, candleSize, candlesAmount);
        }
        
        public RedCandleValidator CreateRedCandleValidator(int candleSize)
        {
            INotificationHandler notificationHandler = new RedCandleNotificationHandler(m_notificationService);
            return new RedCandleValidator(notificationHandler, CurrencyService, candleSize);
        }
        
        public GreenCandleValidator CreateGreenCandleValidator(int candleSize)
        {
            INotificationHandler notificationHandler = new GreenCandleNotificationHandler(m_notificationService);
            return new GreenCandleValidator(notificationHandler, CurrencyService, candleSize);
        }
    }
}