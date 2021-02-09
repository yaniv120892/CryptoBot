using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common.DataStorageObjects;
using Infra;
using Microsoft.Extensions.Logging;
using Storage.Abstractions;
using Utils;

namespace Storage.Repository
{
    public class RepositoryImpl<T> : IRepository<T> where T: StorageObjectBase
    {
        private readonly bool m_deleteOldData;
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<RepositoryImpl<T>>();
        private readonly ConcurrentDictionary<string,ConcurrentDictionary<string,T>> m_mapSymbolTimeToStoredData;

        public RepositoryImpl(Dictionary<string,string> currenciesToCalculatedDataFiles, bool deleteOldData=true)
        {
            m_deleteOldData = deleteOldData;
            m_mapSymbolTimeToStoredData = new ConcurrentDictionary<string, ConcurrentDictionary<string, T>>();
            Initialize(currenciesToCalculatedDataFiles);
        }

        public T Get(string symbol, DateTime time)
        {
            s_logger.LogTrace($"{symbol}_{typeof(T)}: get data at {time}");
            if (!m_mapSymbolTimeToStoredData.TryGetValue(symbol, out ConcurrentDictionary<string, T> mapTimeToStoredData))
            {
                throw new KeyNotFoundException($"Unknown symbol {symbol}");
            }
            
            if (!mapTimeToStoredData.TryGetValue(time.ToString(CultureInfo.InvariantCulture), out T result))
            {
                throw new KeyNotFoundException($"{symbol}_{typeof(T)} for {symbol} at {time} not found");
            }
            
            return result;
        }
        
        public void Add(string symbol, DateTime time, T storedData)
        {
            if (!m_mapSymbolTimeToStoredData.TryGetValue(symbol, out ConcurrentDictionary<string, T> mapTimeToStoredData))
            {
                throw new KeyNotFoundException($"Unknown symbol {symbol}");
            }
            
            mapTimeToStoredData[time.ToString(CultureInfo.InvariantCulture)] = storedData;
            s_logger.LogTrace($"{symbol}_{typeof(T)}: Add data at time {time}");

            if (m_deleteOldData)
            {
                string timeToDelete = time.Subtract(TimeSpan.FromMinutes(15)).ToString(CultureInfo.InvariantCulture);
                if (!mapTimeToStoredData.Remove(timeToDelete, out _))
                {
                    s_logger.LogWarning($"{symbol}_{typeof(T)}: Failed to delete {timeToDelete}, {string.Join(", ",mapTimeToStoredData.Keys)}");
                }
            }
        }
        
        public void Delete(string symbol, DateTime time)
        {
            s_logger.LogTrace($"{symbol}_{typeof(T)}: Delete data at {time}");

            if (!m_mapSymbolTimeToStoredData.TryGetValue(symbol, out _))
            {
                return;
            }
            
            m_mapSymbolTimeToStoredData[symbol].Remove(time.ToString(CultureInfo.InvariantCulture), out _);
        }
        
        public bool TryGet(string symbol, DateTime time, out T storedData)
        {
            try
            {
                s_logger.LogTrace($"{symbol}_{typeof(T)}: Try get data at {time}");
                storedData = Get(symbol, time);
                return true;
            }
            catch (KeyNotFoundException)
            {
                storedData = default(T);
                return false;
            }
        }

        private void Initialize(Dictionary<string, string> currenciesToCalculatedDataFiles)
        {
            foreach (var currency in currenciesToCalculatedDataFiles.Keys)
            {
                if (currenciesToCalculatedDataFiles[currency] == String.Empty || !File.Exists(currenciesToCalculatedDataFiles[currency]))
                {
                    m_mapSymbolTimeToStoredData[currency] = new ConcurrentDictionary<string, T>();
                }
                else
                {
                    InitFromFile(currency,currenciesToCalculatedDataFiles[currency]);
                }
            }
        }

        public T[] GetAll(string symbol)
        {
            return m_mapSymbolTimeToStoredData[symbol].Values.ToArray();
        }
        
        public async Task SaveDataToFileAsync(string symbol, string fileName)
        {
            T[] newCalculated = GetAll(symbol);
            if (File.Exists(fileName))
            {
                T[] oldCalculated = CsvFileAccess.ReadCsv<T>(fileName); 
                s_logger.LogInformation($"Merge old and new data");
                var mergedCalculated = (oldCalculated.Union(newCalculated)).Distinct().ToArray();
                CsvFileAccess.DeleteFile(fileName);
                newCalculated = mergedCalculated;
            }

            await CsvFileAccess.WriteCsvAsync(fileName, newCalculated);
            s_logger.LogInformation($"Done create {fileName}");        
        }

        public void InitFromFile(string symbol, string fileName)
        {
            if (!File.Exists(fileName))
            {
                throw new Exception($"File not found {fileName}");
            }

            T[] oldCalculated = CsvFileAccess.ReadCsv<T>(fileName);
            m_mapSymbolTimeToStoredData[symbol] = new ConcurrentDictionary<string, T>
                (oldCalculated.ToDictionary(m => m.Time.ToString(CultureInfo.InvariantCulture)));
        }

        public DateTime GetLastByTime(string symbol)
        {
            List<DateTime> allDateTimes = m_mapSymbolTimeToStoredData[symbol].Keys.Select(DateTime.Parse).ToList();
            allDateTimes.Sort();
            return allDateTimes.LastOrDefault();
        }
    }
}