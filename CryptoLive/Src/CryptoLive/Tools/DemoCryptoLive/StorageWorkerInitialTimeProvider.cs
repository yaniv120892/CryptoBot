using System;
using System.Globalization;
using Common.DataStorageObjects;
using Storage.Abstractions.Repository;
using Utils;

namespace DemoCryptoLive
{
    internal class StorageWorkerInitialTimeProvider
    {
        internal static readonly DateTime DefaultStorageInitialTime = DateTime.ParseExact("18/02/2021 23:45:59", CsvFileAccess.DateTimeFormat, CultureInfo.InvariantCulture);

        internal static DateTime GetStorageInitialTime(string currency, IRepository<CandleStorageObject> candleRepository)
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