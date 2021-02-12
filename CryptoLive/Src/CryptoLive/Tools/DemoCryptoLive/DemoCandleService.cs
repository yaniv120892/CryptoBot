using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Common;
using CsvHelper;
using Services.Abstractions;

namespace DemoCryptoLive
{
    internal class DemoCandleService : ICandlesService, IPriceService
    {
        private Dictionary<string, Dictionary<DateTime, MyCandle>> m_mapCurrencyToCandle;
        
        public DemoCandleService(IEnumerable<string> currencies, string folderName)
        {
            Initialize(currencies, folderName);
        }

        public void Initialize(IEnumerable<string> currencies, string folderName)
        {
            m_mapCurrencyToCandle = new Dictionary<string, Dictionary<DateTime, MyCandle>>();
            foreach (string currency in currencies)
            {
                string fileName = GetFileName(folderName, currency);
                using (var reader = new StreamReader(fileName))
                {
                    using (var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        csvReader.Configuration.HeaderValidated = null;
                        var candles = csvReader.GetRecords<MyCandle>();
                        var dateTimeToCandle = new Dictionary<DateTime, MyCandle>();
                        foreach (MyCandle candle in candles)
                        {
                            dateTimeToCandle[candle.OpenTime] = candle;
                        }

                        m_mapCurrencyToCandle[currency] = dateTimeToCandle;
                    }
                }
            }
        }

        private static string GetFileName(string folderName, string currency) => Path.Combine(folderName, $"{currency}.csv");

        public Task<Memory<MyCandle>> GetOneMinuteCandles(string currency, int candlesAmount, DateTime currentTime)
        {
            Memory<MyCandle> ans = new MyCandle[candlesAmount];
            DateTime time = currentTime;
            for (int i = ans.Length-1; i >= 0; i-- , time = time.Subtract(TimeSpan.FromMinutes(1)))
            {
                ans.Span[i] = m_mapCurrencyToCandle[currency][time];
            }

            return Task.FromResult(ans);        
        }

        public Task<decimal> GetPrice(string currency, DateTime currentTime) => Task.FromResult(m_mapCurrencyToCandle[currency][currentTime].Close);
    }
}