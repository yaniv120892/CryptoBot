using Utils.CryptoPollings;
using Utils.CryptoValidators;

namespace Utils.Abstractions
{
    public interface ICryptoBotPhasesFactory
    {
        public ICurrencyService CurrencyService { get; }
        public ISystemClock SystemClock { get; }

        RsiPolling CreateRsiPolling(int candleSize, decimal maxRsiToNotify, int candlesAmount);
        RedCandleValidator CreateRedCandleValidator(int candleSize);
        GreenCandleValidator CreateGreenCandleValidator(int candleSize);

        CandlePolling CreateCandlePolling(decimal basePrice, int delayTimeIterationsInSeconds, int candleSize,
            decimal priceChangeToNotify);
    }
}