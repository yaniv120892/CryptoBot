using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Common;
using Services.Abstractions;
using Storage.Repository;
using Utils;

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
                MyCandle[] candles = CsvFileAccess.ReadCsv<MyCandle>(fileName);
                
                var dateTimeToCandle = new Dictionary<DateTime, MyCandle>();
                foreach (MyCandle candle in candles)
                {
                    dateTimeToCandle[candle.CloseTime] = candle;
                }
                m_mapCurrencyToCandle[currency] = dateTimeToCandle;
            }
        }
        
        public Task<Memory<MyCandle>> GetOneMinuteCandles(string currency, int candlesAmount, DateTime currentTime)
        {
            Memory<MyCandle> ans = new MyCandle[candlesAmount];
            DateTime time = RepositoryKeyConverter.AlignTimeToRepositoryKeyFormat(currentTime);
            for (int i = ans.Length-1; i >= 0; i-- , time = time.Subtract(TimeSpan.FromMinutes(1)))
            {
                ans.Span[i] = m_mapCurrencyToCandle[currency][time];
            }

            return Task.FromResult(ans);        
        }

        public Task<Memory<MyCandle>> GetOneMinuteCandles(string currency, DateTime startTime)
        {
            throw new NotImplementedException();
        }

        public Task<decimal> GetPrice(string currency, DateTime currentTime)
        {
            DateTime time = RepositoryKeyConverter.AlignTimeToRepositoryKeyFormat(currentTime);
            return Task.FromResult(m_mapCurrencyToCandle[currency][time].Close);
        }

        private static string GetFileName(string folderName, string currency) => Path.Combine(folderName, $"{currency}.csv");

    }
}