using Binance.Net;
using Binance.Net.Objects.Spot;
using CryptoExchange.Net.Authentication;
using Services.Abstractions;
using Utils.Abstractions;

namespace Services
{
    public class CurrencyClientFactory : ICurrencyClientFactory
    {
        private readonly string m_apiKey;
        private readonly string m_apiSecretKey;

        public CurrencyClientFactory(string apiKey, string apiSecretKey)
        {
            m_apiKey = apiKey;
            m_apiSecretKey = apiSecretKey;
        }

        public BinanceClient Create()
        {
            var client = new BinanceClient(new BinanceClientOptions
            {
                ApiCredentials = new ApiCredentials(m_apiKey, m_apiSecretKey)
            });
            return client;
        }
    }
}