using System;
using System.Threading.Tasks;
using Common.DataStorageObjects;

namespace Storage.Abstractions
{
    public interface IRepositoryUpdater
    {
        void AddInfo(CandleStorageObject candle, DateTime previousTime, DateTime newTime);
        Task PersistDataToFileAsync();
    }
}