using Common.Abstractions;

namespace CryptoBot.Abstractions
{
    public interface ICurrencyBotPhasesExecutorFactory
    {
        CurrencyBotPhasesExecutor Create(ICryptoBotPhasesFactory
            cryptoBotPhasesFactory, 
            CryptoParametersBase cryptoParametersBase);
    }
}