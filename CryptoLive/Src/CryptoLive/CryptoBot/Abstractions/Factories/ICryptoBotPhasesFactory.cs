using System.Collections.Generic;
using System.Threading;
using Common;
using Common.Abstractions;
using CryptoBot.CryptoValidators;
using Utils.Abstractions;

namespace CryptoBot.Abstractions.Factories
{
    public interface ICryptoBotPhasesFactory
    {
        ISystemClock SystemClock { get; }
        CryptoPollingBase CreateCandlePolling(decimal basePrice,
            int delayTimeIterationsInSeconds,
            int candleSize,
            decimal priceChangeToNotify);
        CryptoPollingBase CreatePriceAndRsiPolling(decimal maxRsiToNotify,
            ICryptoPriceAndRsiQueue<PriceAndRsi> cryptoPriceAndRsiQueue,
            Queue<CancellationToken> parentRunningCancellationToken,
            int iterationToRunBeforeWaitingForParentToFinish);
        RedCandleValidator CreateRedCandleValidator(int candleSize);
        GreenCandleValidator CreateGreenCandleValidator(int candleSize);
        IBuyCryptoTrader CreateMarketBuyCryptoTrader();
        ISellCryptoTrader CreateOcoSellCryptoTrader();
    }
}