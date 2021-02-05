using Binance.Net;

namespace Services.Abstractions
{
    public interface ICurrencyClientFactory
    {
        BinanceClient Create();
    }
}