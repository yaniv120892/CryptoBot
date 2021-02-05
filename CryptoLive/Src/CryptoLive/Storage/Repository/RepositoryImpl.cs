using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Storage.Abstractions;

namespace Storage.Repository
{
    public class RepositoryImpl<T> : IRepository<T>
    {
        private readonly ConcurrentDictionary<string,ConcurrentDictionary<DateTime,T>> m_mapSymbolTimeToStoredData;

        public RepositoryImpl(IEnumerable<string> currencies)
        {
            m_mapSymbolTimeToStoredData = new ConcurrentDictionary<string, ConcurrentDictionary<DateTime, T>>();
            Initialize(currencies);
        }

        public T Get(string symbol, DateTime time)
        {
            if (!m_mapSymbolTimeToStoredData.TryGetValue(symbol, out ConcurrentDictionary<DateTime, T> mapTimeToStoredData))
            {
                throw new KeyNotFoundException($"Unknown symbol {symbol}");
            }
            
            if (!mapTimeToStoredData.TryGetValue(time, out T result))
            {
                throw new KeyNotFoundException($"{typeof(T)} for {symbol} at {time} not found");
            }
            
            return result;
        }
        
        public void Add(string symbol, DateTime time, T storedData)
        {
            if (!m_mapSymbolTimeToStoredData.TryGetValue(symbol, out ConcurrentDictionary<DateTime, T> mapTimeToStoredData))
            {
                throw new KeyNotFoundException($"Unknown symbol {symbol}");
            }
            
            mapTimeToStoredData[time] = storedData;
            //TODO: Delete old data if needed
        }
        
        public void Delete(string symbol, DateTime time)
        {
            if (!m_mapSymbolTimeToStoredData.TryGetValue(symbol, out _))
            {
                return;
            }
            
            m_mapSymbolTimeToStoredData[symbol].Remove(time, out _);
        }
        
        public bool TryGet(string symbol, DateTime time, out T storedData)
        {
            try
            {
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
                m_mapSymbolTimeToStoredData[currency] = new ConcurrentDictionary<DateTime, T>();
            }
        }
    }
}