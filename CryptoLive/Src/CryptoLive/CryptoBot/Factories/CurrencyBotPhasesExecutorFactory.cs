using Common.Abstractions;
using CryptoBot.Abstractions.Factories;

namespace CryptoBot.Factories
{
    public class CurrencyBotPhasesExecutorFactory : ICurrencyBotPhasesExecutorFactory
    {
        public CurrencyBotPhasesExecutor Create(ICryptoBotPhasesFactory cryptoBotPhasesFactory,
            CryptoParametersBase cryptoParametersBase) =>
            new CurrencyBotPhasesExecutor(
                cryptoBotPhasesFactory,
                cryptoParametersBase.MaxRsiToNotify,
                cryptoParametersBase.CandleSize,
                cryptoParametersBase.CandleSize,
                cryptoParametersBase.PriceChangeToNotify);
    }
}