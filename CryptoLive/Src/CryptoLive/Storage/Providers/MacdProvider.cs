using System;
using Common.DataStorageObjects;
using Storage.Abstractions;

namespace Storage.Providers
{
    public class MacdProvider : IMacdProvider
    {
        private readonly IRepository<MacdStorageObject> m_macdRepository;

        public MacdProvider(IRepository<MacdStorageObject> macdRepository)
        {
            m_macdRepository = macdRepository;
        }

        public decimal Get(string symbol, DateTime dateTime) => m_macdRepository.Get(symbol, dateTime).MacdHistogram;
    }
}