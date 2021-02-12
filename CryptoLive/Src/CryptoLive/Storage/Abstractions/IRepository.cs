using System;
using System.Threading.Tasks;

namespace Storage.Abstractions
{
    public interface IRepository<T>
    {
        T Get(string currency, DateTime time);
        bool TryGet(string currency, DateTime time, out T storedData);
        void Add(string currency, DateTime time, T storedData);
        void Delete(string currency, DateTime time);
        Task SaveDataToFileAsync(string currency, string fileName);
    }
}