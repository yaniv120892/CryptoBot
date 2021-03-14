using CryptoBot.Abstractions.Factories;
using Services.Abstractions;
using Storage.Abstractions.Providers;
using Storage.Providers;
using Utils.Abstractions;

namespace CryptoBot.Factories
{
    public class CryptoBotPhasesFactoryCreator : ICryptoBotPhasesFactoryCreator
    {
        private readonly ISystemClock m_systemClock;
        private readonly IRsiProvider m_rsiProvider;
        private readonly ICandlesProvider m_candlesProvider;
        private readonly IMacdProvider m_macdProvider;
        private readonly ITradeService m_tradeService;

        public CryptoBotPhasesFactoryCreator(ISystemClock systemClock, 
            IRsiProvider rsiProvider, 
            ICandlesProvider candlesProvider, 
            IMacdProvider macdProvider, 
            ITradeService tradeService)
        {
            m_systemClock = systemClock;
            m_rsiProvider = rsiProvider;
            m_candlesProvider = candlesProvider;
            m_macdProvider = macdProvider;
            m_tradeService = tradeService;
        }
        
        public ICryptoBotPhasesFactory Create()
        {
            var currencyDataProvider = new CurrencyDataProvider(m_candlesProvider, m_rsiProvider, m_macdProvider);
            return new CryptoBotPhasesFactory(currencyDataProvider, m_systemClock, m_tradeService);
        }
    }
}