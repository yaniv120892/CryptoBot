using Common.Abstractions;
using CryptoBot.Abstractions;

namespace CryptoBot.Factories
{
    public class CurrencyBotPhasesExecutorFactory : ICurrencyBotPhasesExecutorFactory
    {
        public CurrencyBotPhasesExecutor Create(ICryptoBotPhasesFactory cryptoBotPhasesFactory,
            CryptoParametersBase cryptoParametersBase)
        {
            return new CurrencyBotPhasesExecutor(
                cryptoBotPhasesFactory,
                cryptoParametersBase.MaxRsiToNotify,
                cryptoParametersBase.RsiMemorySize,
                cryptoParametersBase.CandleSize,
                cryptoParametersBase.CandleSize,
                cryptoParametersBase.CandleSize,
                cryptoParametersBase.DelayTimeIterationsInSeconds,
                cryptoParametersBase.MinutesToWaitBeforePollingPrice,
                cryptoParametersBase.PriceChangeToNotify,
                cryptoParametersBase.CandleSize);
        }
    }
}