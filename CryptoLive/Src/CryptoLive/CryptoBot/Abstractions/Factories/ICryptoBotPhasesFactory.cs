using CryptoBot.CryptoPollings;
using CryptoBot.CryptoValidators;
using Storage.Abstractions.Providers;
using Utils.Abstractions;

namespace CryptoBot.Abstractions.Factories
{
    public interface ICryptoBotPhasesFactory
    {
        ICurrencyDataProvider CurrencyDataProvider { get; }
        ISystemClock SystemClock { get; }
        CandleCryptoPolling CreateCandlePolling(decimal basePrice, 
            int delayTimeIterationsInSeconds, 
            int candleSize,
            decimal priceChangeToNotify);
        public PriceAndRsiCryptoPolling CreatePriceAndRsiPolling(int candleSize, 
            decimal maxRsiToNotify,
            int rsiMemorySize);
        MacdHistogramCryptoPolling CreateMacdPolling(int macdCandleSize, int maxMacdPollingTime);
        RsiCryptoPolling CreateRsiPolling(decimal maxRsiToNotify);
        RedCandleValidator CreateRedCandleValidator(int candleSize);
        GreenCandleValidator CreateGreenCandleValidator(int candleSize);
        MacdHistogramNegativeValidator CreateMacdNegativeValidator(int macdCandleSize);
    }
}