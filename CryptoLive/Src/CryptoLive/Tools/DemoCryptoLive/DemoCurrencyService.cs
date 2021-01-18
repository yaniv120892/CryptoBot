using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Common;
using CsvHelper;
using Utils;
using Utils.Abstractions;

namespace DemoCryptoLive
{
    public class DemoCurrencyService : ICurrencyService
    {
        private readonly Dictionary<string,Dictionary<DateTime, MyCandle>> m_currencyToCandles;

        public DemoCurrencyService(string folderName, string[] currencies)
        {
            m_currencyToCandles = new Dictionary<string, Dictionary<DateTime, MyCandle>>();
            Initialize(folderName, currencies);
        }

        private void Initialize(string folderName, string[] currencies)
        {
            foreach (var currency in currencies)
            {
                string fileName = GetFileName(folderName, currency);
                using var reader = new StreamReader(fileName);
                using var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture);
                csvReader.Configuration.RegisterClassMap<MyCandleMap>();
                csvReader.Configuration.HeaderValidated = null;
                var candles = csvReader.GetRecords<MyCandle>();
                Dictionary<DateTime, MyCandle> dateTimeToCandle = new Dictionary<DateTime, MyCandle>();
                foreach (var candle in candles)
                {
                    dateTimeToCandle[candle.OpenTime] = candle;
                }

                m_currencyToCandles[currency] = dateTimeToCandle;
            }
        }

        public Task<decimal> GetPriceAsync(string desiredSymbol, DateTime currentTime)
        {
            return Task.FromResult(m_currencyToCandles[desiredSymbol][currentTime].Open);
        }

        public async Task<decimal> GetRsiAsync(string desiredSymbol, int candleSizeInMinutes, DateTime currentTime, int candlesAmount)
        {
            MyCandle[] binanceKlinesArr = await GetCandlesAsync(desiredSymbol, candleSizeInMinutes, candlesAmount, currentTime);
            return RsiCalculator.Calculate(binanceKlinesArr);
        }

        private MyCandle[] ReadKlineFromMemory(string desiredSymbol, int limit, DateTime currentTime)
        {
            MyCandle[] ans = new MyCandle[limit];
            DateTime time = currentTime;
            for (int i = 0; i < ans.Length; i++ , time = time.Subtract(TimeSpan.FromMinutes(1)))
            {
                ans[i] = m_currencyToCandles[desiredSymbol][time];
            }

            return ans;
        }

        public async Task<(MyCandle prevCandle, MyCandle currCandle)> GetLastCandlesAsync(string desiredSymbol, int candleSizeInMinutes, DateTime currentTime)
        {
            MyCandle[] lastCandles = await GetCandlesAsync(desiredSymbol, candleSizeInMinutes, 2, currentTime);
            
            var prevCandle = lastCandles[0];
            var currCandle = lastCandles[1];

            return (prevCandle,currCandle);
        }

        public Task<MyCandle[]> GetCandlesAsync(string desiredSymbol, int candleSizeInMinutes, int candlesAmount, DateTime currentTime)
        {
            int limit = candleSizeInMinutes * candlesAmount + 1; // +1 is in order to ignore last candle that didn't finish yet
            MyCandle[] binanceKlinesArr = ReadKlineFromMemory(desiredSymbol ,limit, currentTime);
            MyCandle[] candlesDescription = ConvertByCandleSize(binanceKlinesArr, candleSizeInMinutes, candlesAmount);
            return Task.FromResult(candlesDescription);
        }

        private static MyCandle[] ConvertByCandleSize(MyCandle[] binanceKlines, int candleSizeInMinutes, int candlesAmount)
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
        
        private static (decimal low, decimal high) GetHighAndLow(MyCandle[] binanceKlines, int start, int end)
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
        
        private static string GetFileName(string folderName, string currency) => Path.Combine(folderName, $"{currency}.csv");
    }
}