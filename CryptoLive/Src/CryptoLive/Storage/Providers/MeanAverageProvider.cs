using System;
using Common.DataStorageObjects;
using Storage.Abstractions.Providers;
using Storage.Abstractions.Repository;

namespace Storage.Providers
{
    public class MeanAverageProvider : IMeanAverageProvider
    {
        private readonly IRepository<MeanAverageStorageObject> m_meanAverageRepository;

        public MeanAverageProvider(IRepository<MeanAverageStorageObject> meanAverageRepository)
        {
            m_meanAverageRepository = meanAverageRepository;
        }

        public decimal Get(string currency, DateTime dateTime) => m_meanAverageRepository.Get(currency, dateTime).MeanAverage;
    }
}