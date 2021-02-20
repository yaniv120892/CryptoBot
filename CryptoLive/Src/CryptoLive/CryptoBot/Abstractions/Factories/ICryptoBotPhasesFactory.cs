using CryptoBot.CryptoValidators;
using Storage.Abstractions.Providers;
using Utils.Abstractions;

namespace CryptoBot.Abstractions.Factories
{
    public interface ICryptoBotPhasesFactory
    {
        ICurrencyDataProvider CurrencyDataProvider { get; }
        ISystemClock SystemClock { get; }
        CryptoPollingBase CreateCandlePolling(decimal basePrice, 
            int delayTimeIterationsInSeconds, 
            int candleSize,
            decimal priceChangeToNotify);
        CryptoPollingBase CreatePriceAndRsiPolling(int candleSize, 
            decimal maxRsiToNotify,
            int rsiMemorySize);
        CryptoPollingBase CreateMacdPolling(int macdCandleSize, int maxMacdPollingTime);
        CryptoPollingBase CreateRsiPolling(decimal maxRsiToNotify);
        RedCandleValidator CreateRedCandleValidator(int candleSize);
        GreenCandleValidator CreateGreenCandleValidator(int candleSize);
        MacdHistogramNegativeValidator CreateMacdNegativeValidator(int macdCandleSize);
    }
}