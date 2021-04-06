using System;
using Common.DataStorageObjects;
using Storage.Abstractions.Providers;
using Storage.Abstractions.Repository;
using Storage.Repository;

namespace Storage.Providers
{
    public class RsiProvider : IRsiProvider
    {
        private readonly IRepository<RsiStorageObject> m_rsiRepository;

        public RsiProvider(IRepository<RsiStorageObject> rsiRepository)
        {
            m_rsiRepository = rsiRepository;
        }

        public decimal Get(string currency, DateTime dateTime)
        {
            DateTime time = RepositoryKeyConverter.AlignTimeToRepositoryKeyFormat(dateTime);
            return m_rsiRepository.Get(currency, time).Rsi;
        } 
    }
}