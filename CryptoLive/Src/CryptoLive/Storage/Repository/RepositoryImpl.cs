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
        
        public T[] GetAll(string currency) => 
            m_mapCurrencyTimeToStoredData[currency].Values.ToArray();

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

            var newCalculatedSorted = newCalculated.ToList();
            newCalculatedSorted.Sort();
            await CsvFileAccess.WriteCsvAsync(fileName, newCalculatedSorted);
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
                (oldCalculated.ToDictionary(m => m.Time.ToString(CultureInfo.InvariantCulture)));
        }

        public DateTime GetLastByTime(string currency)
        {
            List<DateTime> allDateTimes = m_mapCurrencyTimeToStoredData[currency].Keys.Select(DateTime.Parse).ToList();
            allDateTimes.Sort();
            return allDateTimes.LastOrDefault();
        }

        public T Get(string currency, DateTime time)
        {
            s_logger.LogTrace($"{currency}_{typeof(T)}: Get {currency}_{time.ToString(CultureInfo.InvariantCulture)}");
            if (!m_mapCurrencyTimeToStoredData.TryGetValue(currency, out ConcurrentDictionary<string, T> mapTimeToStoredData))
            {
                s_logger.LogError($"{currency}_{typeof(T)} Get failed, The given key '{currency}' was not present in the dictionary");
                throw new KeyNotFoundException($"The given key '{currency}' was not present in the dictionary");
            }
            
            if (!mapTimeToStoredData.TryGetValue(time.ToString(CultureInfo.InvariantCulture), out T result))
            {
                s_logger.LogError($"{currency}_{typeof(T)} Get failed, The given key '{time.ToString(CultureInfo.InvariantCulture)}' was not present in the dictionary");
                throw new KeyNotFoundException($"{currency}_{typeof(T)} Get failed, The given key '{time.ToString(CultureInfo.InvariantCulture)}' was not present in the dictionary");
            }
            
            return result;
        }
        
        public void Add(string currency, DateTime time, T storedData)
        {
            s_logger.LogTrace($"{currency}_{typeof(T)}: Add {currency}_{time.ToString(CultureInfo.InvariantCulture)} {storedData}");
            if (!m_mapCurrencyTimeToStoredData.TryGetValue(currency, out ConcurrentDictionary<string, T> mapTimeToStoredData))
            {
                s_logger.LogError($"{currency}_{typeof(T)} Add failed, The given key '{currency}' was not present in the dictionary");
                throw new KeyNotFoundException($"The given key '{currency}' was not present in the dictionary");            
            }
            
            mapTimeToStoredData[time.ToString(CultureInfo.InvariantCulture)] = storedData;

            if (m_deleteOldData)
            {
                string timeToDelete = time.Subtract(TimeSpan.FromMinutes(60)).ToString(CultureInfo.InvariantCulture);
                DeleteOldDataIfExists(currency, mapTimeToStoredData, timeToDelete);
            }
        }

        public bool TryGet(string currency, DateTime time, out T storedData)
        {
            s_logger.LogTrace($"{currency}_{typeof(T)}: TryGet {currency}_{time.ToString(CultureInfo.InvariantCulture)}");
            try
            {
                storedData = m_mapCurrencyTimeToStoredData[currency][time.ToString(CultureInfo.InvariantCulture)];
                return true;
            }
            catch (KeyNotFoundException e)
            {
                s_logger.LogTrace($"{currency}_{typeof(T)}: TryGet failed, {e.Message}");
                storedData = default(T);
                return false;
            }
            catch (Exception e)
            {
                s_logger.LogTrace($"{currency}_{typeof(T)}: TryGet failed, {e.Message}");
                throw;
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
        
        private static void DeleteOldDataIfExists(string currency, ConcurrentDictionary<string, T> mapTimeToStoredData,
            string timeToDelete)
        {
            s_logger.LogTrace($"{currency}_{typeof(T)}: Delete {currency}_{timeToDelete}");
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
                $"{currency}_{typeof(T)}: Delete {timeToDelete} failed , {string.Join(", ", keys )}");
        }
    }
}