using System;
using System.Linq;
using System.Threading.Tasks;
using Binance.Net;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Spot;
using Common;
using CryptoExchange.Net.Authentication;
using Infra;
using Microsoft.Extensions.Logging;
using Utils.Abstractions;

namespace Utils.CurrencyService
{
    public class BinanceCurrencyService : ICurrencyService
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<BinanceCurrencyService>();
        
        private readonly string m_apiKey;
        private readonly string m_apiSecretKey;

        public BinanceCurrencyService(string apiKey, string apiSecretKey)
        {
            m_apiKey = apiKey;
            m_apiSecretKey = apiSecretKey;
        }
        
        public async Task<decimal> GetPriceAsync(string desiredSymbol, DateTime currentTime)
        {
            try
            {
                s_logger.LogDebug($"Start get price for {desiredSymbol}");
                decimal price = await GetPriceImpl(desiredSymbol);
                s_logger.LogDebug($"Done get price for {desiredSymbol}, price is {price}");
                return price;
            }
            catch (Exception e)
            {
                s_logger.LogError($"Failed get price for {desiredSymbol}, {e.Message}");
                throw new Exception($"Failed get price for {desiredSymbol}");
            }
        }
        
        public async Task<decimal> GetRsiAsync(string desiredSymbol, int candleSizeInMinutes, DateTime currentTime,
            int candlesAmount)
        {
            try
            {
                s_logger.LogDebug($"Start get rsi for {desiredSymbol} with candle size {candleSizeInMinutes}");
                decimal rsi = await GetRsiImpl(desiredSymbol, candleSizeInMinutes, candlesAmount , currentTime);
                s_logger.LogDebug($"Done get rsi for {desiredSymbol} with candle size {candleSizeInMinutes}, rsi is {rsi}");
                return rsi;
            }
            catch (Exception e)
            {
                s_logger.LogError($"Failed get rsi for {desiredSymbol}, {e.Message}");
                throw new Exception($"Failed get rsi for {desiredSymbol}");
            }
        }

        public async Task<(MyCandle prevCandle, MyCandle currCandle)> GetLastCandlesAsync(string desiredSymbol, int candleSizeInMinutes, DateTime currentTime)
        {
            MyCandle[] lastCandles = await GetCandlesAsync(desiredSymbol, candleSizeInMinutes, 2, currentTime);
            
            var prevCandle = lastCandles[0];
            var currCandle = lastCandles[1];

            return (prevCandle,currCandle);
        }
        
        public async Task<MyCandle[]> GetCandlesAsync(string desiredSymbol, int candleSizeInMinutes, int candlesAmount, DateTime currentTime)
        {
            BinanceClient client = CreateBinanceClient();
            string symbol = desiredSymbol;
            KlineInterval interval = KlineInterval.OneMinute;
            int limit = candleSizeInMinutes * candlesAmount + 1; // +1 is in order to ignore last candle that didn't finish yet
            var response = await client.Spot.Market.GetKlinesAsync(symbol, interval, limit:limit);
            IBinanceKline[] binanceKlinesArr = response.Data as IBinanceKline[] ?? response.Data.ToArray();
            MyCandle[] candlesDescription = ConvertByCandleSize(binanceKlinesArr, candleSizeInMinutes, candlesAmount);
            return candlesDescription;
        }

        private async Task<decimal> GetPriceImpl(string desiredSymbol)
        {
            BinanceClient client = CreateBinanceClient();
            var response = await client.Spot.Market.GetPriceAsync(desiredSymbol);
            decimal currentPrice = response.Data.Price;
            return currentPrice;
        }
        
        private async Task<decimal> GetRsiImpl(string desiredSymbol, int candleSizeInMinutes, int candlesAmount , DateTime currentTime)
        {
            MyCandle[] binanceKlines = await GetCandlesAsync(desiredSymbol, candleSizeInMinutes, candlesAmount, currentTime);
            decimal rsi = RsiCalculator.Calculate(binanceKlines);
            return rsi;
        }

        private static MyCandle[] ConvertByCandleSize(IBinanceKline[] binanceKlines, int candleSizeInMinutes, int candlesAmount)
        {
            MyCandle[] ans = new MyCandle[candlesAmount];
            for (int i = 0; i < ans.Length; i++)
            {
                int start = i * candleSizeInMinutes;
                int end = start + candleSizeInMinutes - 1;
                (decimal low, decimal high) = GetHighAndLow(binanceKlines, start, end);
                ans[i] = new MyCandle(binanceKlines[start].Open, binanceKlines[end].Close, binanceKlines[start].OpenTime, binanceKlines[end].CloseTime, low, high);
            }

            return ans;
        }

        private static (decimal low, decimal high) GetHighAndLow(IBinanceKline[] binanceKlines, int start, int end)
        {
            decimal high = Decimal.MinValue;
            decimal low = Decimal.MaxValue;
            for (int i = start; i <= end; i++)
            {
                low = Math.Min(low, binanceKlines[i].Low);
                high = Math.Max(high, binanceKlines[i].High);
            }

            return (low,high);
        }

        private BinanceClient CreateBinanceClient()
        {
            var client = new BinanceClient(new BinanceClientOptions
            {
                ApiCredentials = new ApiCredentials(m_apiKey, m_apiSecretKey)
            });
            return client;
        }
    }
}
