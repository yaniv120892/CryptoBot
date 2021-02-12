using CryptoBot.Abstractions;
using Infra;
using Storage.Abstractions;
using Storage.Abstractions.Providers;
using Storage.Providers;
using Utils.Abstractions;

namespace CryptoBot.Factories
{
    public class CryptoBotPhasesFactoryCreator : ICryptoBotPhasesFactoryCreator
    {
        private readonly ISystemClock m_systemClock;
        private readonly IPriceProvider m_priceService;
        private readonly INotificationService m_notificationService;
        private readonly IRsiProvider m_rsiProvider;
        private readonly ICandlesProvider m_candlesProvider;
        private readonly IMacdProvider m_macdProvider;

        public CryptoBotPhasesFactoryCreator(ISystemClock systemClock, 
            IPriceProvider priceService, 
            IRsiProvider rsiProvider, 
            ICandlesProvider candlesProvider, 
            IMacdProvider macdProvider,
            INotificationService notificationService)
        {
            m_systemClock = systemClock;
            m_priceService = priceService;
            m_rsiProvider = rsiProvider;
            m_candlesProvider = candlesProvider;
            m_notificationService = notificationService;
            m_macdProvider = macdProvider;
        }
        
        public ICryptoBotPhasesFactory Create()
        {
            var currencyDataProvider = new CurrencyDataProvider(m_priceService, m_candlesProvider, m_rsiProvider, m_macdProvider);
            return new CryptoBotPhasesFactory(currencyDataProvider, m_systemClock, m_notificationService);
        }
    }
}