using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using Infra;
using Microsoft.Extensions.Logging;
using Storage.Abstractions;

namespace Storage.Repository
{
    public class RepositoryImpl<T> : IRepository<T>
    {
        private readonly bool m_deleteOldData;
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<RepositoryImpl<T>>();
        private readonly ConcurrentDictionary<string,ConcurrentDictionary<string,T>> m_mapSymbolTimeToStoredData;

        public RepositoryImpl(IEnumerable<string> currencies, bool deleteOldData=true)
        {
            m_deleteOldData = deleteOldData;
            m_mapSymbolTimeToStoredData = new ConcurrentDictionary<string, ConcurrentDictionary<string, T>>();
            Initialize(currencies);
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
        
        private void Initialize(IEnumerable<string> currencies)
        {
            foreach (var currency in currencies)
            {
                m_mapSymbolTimeToStoredData[currency] = new ConcurrentDictionary<string, T>();
            }
        }
    }
}