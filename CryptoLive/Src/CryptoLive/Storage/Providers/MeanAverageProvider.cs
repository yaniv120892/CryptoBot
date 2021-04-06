using System;
using Common.DataStorageObjects;
using Storage.Abstractions.Providers;
using Storage.Abstractions.Repository;
using Storage.Repository;

namespace Storage.Providers
{
    public class MeanAverageProvider : IMeanAverageProvider
    {
        private readonly IRepository<MeanAverageStorageObject> m_meanAverageRepository;

        public MeanAverageProvider(IRepository<MeanAverageStorageObject> meanAverageRepository)
        {
            m_meanAverageRepository = meanAverageRepository;
        }

        public decimal Get(string currency, DateTime dateTime)
        {
            DateTime time = RepositoryKeyConverter.AlignTimeToRepositoryKeyFormat(dateTime);
            return m_meanAverageRepository.Get(currency, time).MeanAverage;
        }
    }
}