using System;
using System.Threading.Tasks;

namespace Storage.Abstractions
{
    public interface IRepository<T>
    {
        T Get(string symbol, DateTime time);
        bool TryGet(string symbol, DateTime time, out T storedData);
        void Add(string symbol, DateTime time, T storedData);
        void Delete(string symbol, DateTime time);
        Task SaveDataToFileAsync(string symbol, string fileName);
    }
}