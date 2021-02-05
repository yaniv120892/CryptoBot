using System;
using Common.DataStorageObjects;
using Storage.Abstractions;

namespace Storage.Providers
{
    public class RsiProvider : IRsiProvider
    {
        private readonly IRepository<RsiStorageObject> m_rsiRepository;

        public RsiProvider(IRepository<RsiStorageObject> rsiRepository)
        {
            m_rsiRepository = rsiRepository;
        }

        public decimal Get(string symbol, DateTime dateTime) => m_rsiRepository.Get(symbol, dateTime).Rsi;
    }
}