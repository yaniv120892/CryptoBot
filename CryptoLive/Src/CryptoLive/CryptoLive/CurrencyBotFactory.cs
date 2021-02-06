using CryptoBot;
using CryptoBot.Abstractions;

namespace CryptoLive
{
    public class CurrencyBotFactory
    {
        private static readonly int s_minutesToWaitBeforePollingPrice = 1;
        private static readonly int s_priceChangeDelayTimeIterationsInSeconds = 60;

        
        public static CurrencyBot Create(CryptoLiveParameters cryptoLiveParameters, ICryptoBotPhasesFactory cryptoBotPhasesFactory, string currency)
        {
            CurrencyBotPhasesExecutor currencyBotPhasesExecutor = new CurrencyBotPhasesExecutor(
                cryptoBotPhasesFactory, 
                cryptoLiveParameters.MaxRsiToNotify,
                cryptoLiveParameters.RsiMemorySize,
                cryptoLiveParameters.CandleSize,
                cryptoLiveParameters.CandleSize,
                cryptoLiveParameters.CandleSize,
                s_priceChangeDelayTimeIterationsInSeconds,
                s_minutesToWaitBeforePollingPrice,
                cryptoLiveParameters.PriceChangeToNotify,
                cryptoLiveParameters.CandleSize);
            return new CurrencyBot(currencyBotPhasesExecutor, currency);
        }
    }
    
    
}