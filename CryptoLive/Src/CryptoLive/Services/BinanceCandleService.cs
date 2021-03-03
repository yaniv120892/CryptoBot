using System;
using System.Linq;
using System.Threading.Tasks;
using Binance.Net;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Common;
using Infra;
using Microsoft.Extensions.Logging;
using Services.Abstractions;
using Utils.Converters;

namespace Services
{
    public class BinanceCandleService : ICandlesService
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<BinanceCandleService>();

        private readonly ICurrencyClientFactory m_currencyClientFactory;
        
        public BinanceCandleService(ICurrencyClientFactory currencyClientFactory)
        {
            m_currencyClientFactory = currencyClientFactory;
        }

        public async Task<Memory<MyCandle>> GetOneMinuteCandles(string currency, int candlesAmount, DateTime currentTime)
        {
            BinanceClient client = m_currencyClientFactory.Create();
            KlineInterval interval = KlineInterval.OneMinute;
            try
            {
                var response = await client.Spot.Market.GetKlinesAsync(currency, interval, limit: candlesAmount);
                IBinanceKline[] binanceKlinesArr = response.Data as IBinanceKline[] ?? response.Data.ToArray();
                Memory<MyCandle> candlesDescription =
                    BinanceKlineToMyCandleConverter.ConvertByCandleSize(binanceKlinesArr, 1, candlesAmount);
                return candlesDescription;
            }
            catch (Exception e)
            {
                s_logger.LogError(e,$"Failed to get {candlesAmount} candles for {currency}");
                throw;
            }
            
        }
    }
}