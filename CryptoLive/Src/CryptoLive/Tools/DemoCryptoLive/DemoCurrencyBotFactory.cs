using CryptoBot;
using CryptoBot.Abstractions;

namespace DemoCryptoLive
{
    public class DemoCurrencyBotFactory
    {

        public static CurrencyBot Create(DemoCryptoParameters appParameters,
            ICryptoBotPhasesFactory cryptoBotPhasesFactory, string currency)
        {
            int minutesToWaitBeforePollingPrice = appParameters.CandleSize;
            return new CurrencyBot(cryptoBotPhasesFactory, 
                currency,
                appParameters.MaxRsiToNotify,
                appParameters.CandleSize,
                appParameters.CandleSize,
                appParameters.CandleSize,
                appParameters.DelayTimeIterationsInSeconds,
                appParameters.PriceChangeToNotify,
                appParameters.CandleSize,
                appParameters.RsiMemorySize,
                appParameters.CandleSize,
                minutesToWaitBeforePollingPrice,
                appParameters.MaxMacdPollingTime);
        }
    }
}