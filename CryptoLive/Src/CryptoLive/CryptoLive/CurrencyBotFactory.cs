using CryptoBot;
using CryptoBot.Abstractions;

namespace CryptoLive
{
    public class CurrencyBotFactory
    {
        private static readonly int s_minutesToWaitBeforePollingPrice = 1;
        
        public static CurrencyBot Create(CryptoLiveParameters cryptoLiveParameters, ICryptoBotPhasesFactory cryptoBotPhasesFactory, string currency) =>
            new CurrencyBot(cryptoBotPhasesFactory, currency, 
                cryptoLiveParameters.MaxRsiToNotify,
                cryptoLiveParameters.CandleSize, 
                cryptoLiveParameters.CandleSize, 
                cryptoLiveParameters.CandleSize, 
                60, 
                cryptoLiveParameters.PriceChangeToNotify,
                1,
                cryptoLiveParameters.RsiMemorySize,
                cryptoLiveParameters.CandleSize, 
                s_minutesToWaitBeforePollingPrice,
                cryptoLiveParameters.MaxMacdPollingTime);
    }
    
    
}