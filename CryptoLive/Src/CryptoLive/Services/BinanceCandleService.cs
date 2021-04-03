using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Binance.Net;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Common;
using CryptoExchange.Net.Objects;
using Infra;
using Microsoft.Extensions.Logging;
using Services.Abstractions;
using Utils.Converters;

namespace Services
{
    public class BinanceCandleService : ICandlesService
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<BinanceCandleService>();
        private static readonly KlineInterval s_klineInterval = KlineInterval.OneMinute;

        private readonly ICurrencyClientFactory m_currencyClientFactory;
        
        public BinanceCandleService(ICurrencyClientFactory currencyClientFactory)
        {
            m_currencyClientFactory = currencyClientFactory;
        }

        public async Task<Memory<MyCandle>> GetOneMinuteCandles(string currency, int candlesAmount,
            DateTime currentTime)
        {
            try
            {
                BinanceClient client = m_currencyClientFactory.Create();
                var response = await HttpRequestRetryHandler.RetryOnFailure(
                    async () => await client.Spot.Market.GetKlinesAsync(currency, 
                        s_klineInterval, 
                        limit: candlesAmount)
                    ,"GetOneMinuteCandles");
                return ExtractCandlesFromResponse(response, candlesAmount);
            }
            catch (Exception e)
            {
                s_logger.LogError(e,$"Failed to get {candlesAmount} candles for {currency}: {e.Message}");
                throw new Exception($"Failed to get {candlesAmount} candles for {currency}");
            }
        }
        
        public async Task<Memory<MyCandle>> GetOneMinuteCandles(string currency, DateTime startTime)
        {
            const int candlesAmount = 999;
            try
            {
                BinanceClient client = m_currencyClientFactory.Create();
                var response = await HttpRequestRetryHandler.RetryOnFailure(
                    async () => await client.Spot.Market.GetKlinesAsync(currency, s_klineInterval, 
                        limit:candlesAmount, startTime: startTime),
                    "GetOneMinuteCandles");
                return ExtractCandlesFromResponse(response, candlesAmount);
            }
            catch (Exception e)
            {
                s_logger.LogError(e,$"Failed to get {candlesAmount} candles for {currency} from {startTime:dd/MM/yyyy HH:mm:ss}");
                throw new Exception($"Failed to get {candlesAmount} candles for {currency} from {startTime:dd/MM/yyyy HH:mm:ss}");
            }
        }
        
        private static Memory<MyCandle> ExtractCandlesFromResponse(CallResult<IEnumerable<IBinanceKline>> response,
            int candlesAmount)
        {
            IBinanceKline[] binanceKlinesArr = response.Data as IBinanceKline[] ?? response.Data.ToArray();
            Memory<MyCandle> candlesDescription =
                CandleConverter.ConvertByCandleSize(binanceKlinesArr, 1, candlesAmount);
            return candlesDescription;
        }
    }
}