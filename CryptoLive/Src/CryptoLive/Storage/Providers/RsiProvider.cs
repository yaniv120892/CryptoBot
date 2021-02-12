using System;
using Common.DataStorageObjects;
using Storage.Abstractions;
using Storage.Abstractions.Providers;
using Storage.Abstractions.Repository;

namespace Storage.Providers
{
    public class RsiProvider : IRsiProvider
    {
        private readonly IRepository<RsiStorageObject> m_rsiRepository;

        public RsiProvider(IRepository<RsiStorageObject> rsiRepository)
        {
            m_rsiRepository = rsiRepository;
        }

        public decimal Get(string currency, DateTime dateTime) => m_rsiRepository.Get(currency, dateTime).Rsi;
    }
}