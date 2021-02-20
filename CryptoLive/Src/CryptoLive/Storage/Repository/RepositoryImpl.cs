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
using Storage.Abstractions.Repository;
using Utils;

namespace Storage.Repository
{
    public class RepositoryImpl<T> : IRepository<T> where T: StorageObjectBase
    {
        private readonly bool m_deleteOldData;
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<RepositoryImpl<T>>();
        private readonly ConcurrentDictionary<string,ConcurrentDictionary<string,T>> m_mapCurrencyTimeToStoredData;

        public RepositoryImpl(Dictionary<string,string> currenciesToCalculatedDataFiles, bool deleteOldData=true)
        {
            m_deleteOldData = deleteOldData;
            m_mapCurrencyTimeToStoredData = new ConcurrentDictionary<string, ConcurrentDictionary<string, T>>();
            Initialize(currenciesToCalculatedDataFiles);
        }

        public T Get(string currency, DateTime time)
        {
            s_logger.LogTrace($"{currency}_{typeof(T)}: get data at {time}");
            if (!m_mapCurrencyTimeToStoredData.TryGetValue(currency, out ConcurrentDictionary<string, T> mapTimeToStoredData))
            {
                throw new KeyNotFoundException($"Unknown currency {currency}");
            }
            
            if (!mapTimeToStoredData.TryGetValue(time.ToString(CultureInfo.InvariantCulture), out T result))
            {
                throw new KeyNotFoundException($"{currency}_{typeof(T)} for {currency} at {time} not found");
            }
            
            return result;
        }
        
        public void Add(string currency, DateTime time, T storedData)
        {
            if (!m_mapCurrencyTimeToStoredData.TryGetValue(currency, out ConcurrentDictionary<string, T> mapTimeToStoredData))
            {
                throw new KeyNotFoundException($"Unknown currency {currency}");
            }
            
            mapTimeToStoredData[time.ToString(CultureInfo.InvariantCulture)] = storedData;
            s_logger.LogTrace($"{currency}_{typeof(T)}: Add data at time {time}");

            if (m_deleteOldData)
            {
                string timeToDelete = time.Subtract(TimeSpan.FromMinutes(15)).ToString(CultureInfo.InvariantCulture);
                DeleteOldDataIfExists(currency, mapTimeToStoredData, timeToDelete);
            }
        }

        public void Delete(string currency, DateTime time)
        {
            s_logger.LogTrace($"{currency}_{typeof(T)}: Delete data at {time}");

            if (!m_mapCurrencyTimeToStoredData.TryGetValue(currency, out _))
            {
                return;
            }
            
            m_mapCurrencyTimeToStoredData[currency].Remove(time.ToString(CultureInfo.InvariantCulture), out _);
        }
        
        public bool TryGet(string currency, DateTime time, out T storedData)
        {
            try
            {
                s_logger.LogTrace($"{currency}_{typeof(T)}: Try get data at {time}");
                storedData = Get(currency, time);
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
                    m_mapCurrencyTimeToStoredData[currency] = new ConcurrentDictionary<string, T>();
                }
                else
                {
                    InitFromFile(currency,currenciesToCalculatedDataFiles[currency]);
                }
            }
        }

        public T[] GetAll(string currency)
        {
            return m_mapCurrencyTimeToStoredData[currency].Values.ToArray();
        }
        
        public async Task SaveDataToFileAsync(string currency, string fileName)
        {
            T[] newCalculated = GetAll(currency);
            if (File.Exists(fileName))
            {
                T[] oldCalculated = CsvFileAccess.ReadCsv<T>(fileName); 
                s_logger.LogInformation($"Merge old and new data");
                var mergedCalculated = (oldCalculated.Union(newCalculated)).Distinct().ToArray();
                CsvFileAccess.DeleteFile(fileName);
                newCalculated = mergedCalculated;
            }

            newCalculated.ToList().Sort((x, y) => x.Time.CompareTo(y.Time));
            await CsvFileAccess.WriteCsvAsync(fileName, newCalculated);
            s_logger.LogInformation($"Done create {fileName}");        
        }

        public void InitFromFile(string currency, string fileName)
        {
            if (!File.Exists(fileName))
            {
                throw new Exception($"File not found {fileName}");
            }

            T[] oldCalculated = CsvFileAccess.ReadCsv<T>(fileName);
            m_mapCurrencyTimeToStoredData[currency] = new ConcurrentDictionary<string, T>
                (oldCalculated.ToDictionary(m => m.Time.AddSeconds(-m.Time.Second).ToString(CultureInfo.InvariantCulture)));
        }

        public DateTime GetLastByTime(string currency)
        {
            List<DateTime> allDateTimes = m_mapCurrencyTimeToStoredData[currency].Keys.Select(DateTime.Parse).ToList();
            allDateTimes.Sort();
            return allDateTimes.LastOrDefault();
        }
        
        private static void DeleteOldDataIfExists(string currency, ConcurrentDictionary<string, T> mapTimeToStoredData,
            string timeToDelete)
        {
            if (!mapTimeToStoredData.TryGetValue(timeToDelete, out _))
            {
                return;
            }

            if (mapTimeToStoredData.Remove(timeToDelete, out _))
            {
                return;
            }

            var keys = mapTimeToStoredData.Keys.ToList();
            keys.Sort();
            s_logger.LogWarning(
                $"{currency}_{typeof(T)}: Failed to delete {timeToDelete}, {string.Join(", ", keys )}");
        }
    }
}