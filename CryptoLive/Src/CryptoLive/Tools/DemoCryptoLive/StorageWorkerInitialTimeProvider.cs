using System;
using System.Globalization;
using Common.DataStorageObjects;
using Storage.Abstractions.Repository;
using Utils;

namespace DemoCryptoLive
{
    internal class StorageWorkerInitialTimeProvider
    {
        internal static readonly DateTime DefaultStorageInitialTime = DateTime.ParseExact("06/03/2021 12:00:00", CsvFileAccess.DateTimeFormat, CultureInfo.InvariantCulture);
        internal static readonly DateTime DefaultStorageEndTime = DateTime.ParseExact("24/03/2021 02:00:00", CsvFileAccess.DateTimeFormat, CultureInfo.InvariantCulture);

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