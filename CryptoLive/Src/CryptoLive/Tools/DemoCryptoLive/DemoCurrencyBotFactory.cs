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
            CurrencyBotPhasesExecutor currencyBotPhasesExecutor = new CurrencyBotPhasesExecutor(
                cryptoBotPhasesFactory,
                appParameters.MaxRsiToNotify,
                appParameters.RsiMemorySize,
                appParameters.CandleSize,
                appParameters.CandleSize,
                appParameters.CandleSize,
                appParameters.DelayTimeIterationsInSeconds,
                minutesToWaitBeforePollingPrice,
                appParameters.PriceChangeToNotify,
                appParameters.CandleSize);
            return new CurrencyBot(currencyBotPhasesExecutor, currency);
        }
    }
}