using Common.Abstractions;

namespace CryptoBot.Abstractions.Factories
{
    public interface ICurrencyBotPhasesExecutorFactory
    {
        CurrencyBotPhasesExecutor Create(ICryptoBotPhasesFactory
            cryptoBotPhasesFactory, 
            CryptoParametersBase cryptoParametersBase);
    }
}