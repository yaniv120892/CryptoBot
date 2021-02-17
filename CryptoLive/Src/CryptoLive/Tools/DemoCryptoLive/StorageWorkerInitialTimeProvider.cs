using System;
using Common.DataStorageObjects;
using Storage.Repository;

namespace DemoCryptoLive
{
    internal class StorageWorkerInitialTimeProvider
    {
        internal static readonly DateTime DefaultStorageInitialTime = DateTime.Parse("2/2/2021  12:59:00 PM");

        internal static DateTime GetStorageInitialTime(string currency, RepositoryImpl<CandleStorageObject> candleRepository)
        {
            DateTime lastSavedCandleTime = candleRepository.GetLastByTime(currency);
            if (default == lastSavedCandleTime)
            {
                return DefaultStorageInitialTime;
            }

            return lastSavedCandleTime;
        }
    }
}