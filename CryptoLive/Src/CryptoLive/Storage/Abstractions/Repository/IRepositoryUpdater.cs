using System;
using System.Threading.Tasks;
using Common.DataStorageObjects;

namespace Storage.Abstractions.Repository
{
    public interface IRepositoryUpdater
    {
        void AddInfo(CandleStorageObject candle, DateTime newTime);
        Task PersistDataToFileAsync();
    }
}