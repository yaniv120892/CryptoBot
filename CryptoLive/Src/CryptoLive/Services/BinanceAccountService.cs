using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Binance.Net;
using Binance.Net.Objects.Spot.WalletData;
using CryptoExchange.Net.Objects;
using Infra;
using Microsoft.Extensions.Logging;
using Services.Abstractions;

namespace Services
{
    public class BinanceAccountService : IAccountService
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<BinanceAccountService>();

        private readonly ICurrencyClientFactory m_currencyClientFactory;

        public BinanceAccountService(ICurrencyClientFactory currencyClientFactory)
        {
            m_currencyClientFactory = currencyClientFactory;
        }

        public async Task<decimal> GetAvailableUsdt()
        {
            try
            {
                BinanceClient client = m_currencyClientFactory.Create();
                var response = await HttpRequestRetryHandler.RetryOnFailure(
                    async () => await client.General.GetUserCoinsAsync(),
                    "GetAvailableUsdt");
                return ExtractAvailableUsdtFromResponse(response);
            }
            catch (Exception e)
            {
                string message = $"Failed to get available USDT info";
                s_logger.LogError(e, message);
                throw new Exception(message);
            }
        }

        private static decimal ExtractAvailableUsdtFromResponse(WebCallResult<IEnumerable<BinanceUserCoin>> response)
        {
            var usdtInfo = response.Data.SingleOrDefault(m => m.Coin.Equals("USDT"));
            if (usdtInfo == default)
            {
                throw new Exception("USDT coin not found");
            }

            return usdtInfo.Free;
        }
    }
}