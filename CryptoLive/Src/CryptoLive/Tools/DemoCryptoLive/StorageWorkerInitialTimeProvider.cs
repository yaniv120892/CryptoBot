using System;
using Common.DataStorageObjects;
using Storage.Abstractions.Repository;

namespace DemoCryptoLive
{
    internal class StorageWorkerInitialTimeProvider
    {
        internal static DateTime GetStorageInitialTime(string currency, 
            IRepository<CandleStorageObject> candleRepository,
            DateTime defaultStorageInitialTime,
            DateTime storageWorkerEndTime)
        {
            DateTime lastSavedCandleTime = candleRepository.GetLastByTime(currency);
            if (default == lastSavedCandleTime)
            {
                return defaultStorageInitialTime;
            }

            if (lastSavedCandleTime < storageWorkerEndTime)
            {
                return lastSavedCandleTime;
            }
            
            return storageWorkerEndTime;
        }
    }
}