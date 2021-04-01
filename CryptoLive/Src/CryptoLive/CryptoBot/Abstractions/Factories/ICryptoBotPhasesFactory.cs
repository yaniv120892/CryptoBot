using System.Collections.Generic;
using System.Threading;
using Common;
using Common.Abstractions;
using CryptoBot.CryptoValidators;
using Storage.Abstractions.Providers;
using Utils.Abstractions;

namespace CryptoBot.Abstractions.Factories
{
    public interface ICryptoBotPhasesFactory
    {
        ISystemClock SystemClock { get; }
        ICurrencyDataProvider CurrencyDataProvider { get; }
        ICryptoPolling CreateCandlePolling(decimal basePrice, int candleSize, decimal priceChangeToNotify);
        ICryptoPolling CreateOrderStatusPolling(long orderId);
        ICryptoPolling CreatePriceAndRsiPolling(decimal maxRsiToNotify, ICryptoPriceAndRsiQueue<PriceAndRsi> cryptoPriceAndRsiQueue,
            Queue<CancellationToken> parentRunningCancellationToken, int iterationToRunBeforeWaitingForParentToFinish);
        RedCandleValidator CreateRedCandleValidator();
        GreenCandleValidator CreateGreenCandleValidator();
        IBuyCryptoTrader CreateStopLimitBuyCryptoTrader();
        ISellCryptoTrader CreateOcoSellCryptoTrader();
        ICancelOrderCryptoTrader CreateCancelOrderCryptoTrader();
    }
}