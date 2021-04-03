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
        private readonly DateTime m_endTime;
        private Dictionary<string, Dictionary<DateTime, MyCandle>> m_mapCurrencyToCandle;
        
        public DemoCandleService(IEnumerable<string> currencies, string folderName, DateTime endTime)
        {
            m_endTime = endTime;
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
                MyCandle prevCandle = candles[0];
                foreach (MyCandle candle in candles)
                {
                    int minutesToAdd = 1;
                    MyCandle candleWithFixedSecondsRange = MyCandle.GetCandleWithFixedSecondsRange(candle);
                    while (prevCandle.CloseTime.AddMinutes(minutesToAdd) < candleWithFixedSecondsRange.CloseTime)
                    {
                        dateTimeToCandle[prevCandle.CloseTime.AddMinutes(minutesToAdd)] = 
                            MyCandle.CloneWithNewTime(prevCandle, minutesToAdd);
                        minutesToAdd++;
                    }
                    dateTimeToCandle[candleWithFixedSecondsRange.CloseTime] = candleWithFixedSecondsRange;
                    prevCandle = candleWithFixedSecondsRange;
                }
                m_mapCurrencyToCandle[currency] = dateTimeToCandle;
            }
        }

        public Task<Memory<MyCandle>> GetOneMinuteCandles(string currency, int candlesAmount, DateTime currentTime)
        {
            Memory<MyCandle> ans = new MyCandle[candlesAmount];
            DateTime time = RepositoryKeyConverter.AlignTimeToRepositoryKeyFormat(currentTime);
            for (int i = ans.Length - 1; i >= 0; i--, time = time.Subtract(TimeSpan.FromMinutes(1)))
            { 
                if (currentTime > m_endTime)
                {
                    throw new Exception($"No data available for {currentTime:dd/MM/yyyy HH:mm:ss}, endTime is {m_endTime:dd/MM/yyyy HH:mm:ss}");
                }
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
            if (currentTime > m_endTime)
            {
                throw new Exception($"No data available for {currentTime:dd/MM/yyyy HH:mm:ss}, endTime is {m_endTime:dd/MM/yyyy HH:mm:ss}");
            }

            DateTime time = RepositoryKeyConverter.AlignTimeToRepositoryKeyFormat(currentTime);
            return Task.FromResult(m_mapCurrencyToCandle[currency][time].Close);
        }

        private static string GetFileName(string folderName, string currency) => Path.Combine(folderName, $"{currency}.csv");

    }
}