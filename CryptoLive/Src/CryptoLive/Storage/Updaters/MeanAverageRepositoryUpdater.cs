using System;
using System.Threading.Tasks;
using Common.DataStorageObjects;
using Infra;
using Microsoft.Extensions.Logging;
using Storage.Abstractions.Repository;

namespace Storage.Updaters
{
    public class MeanAverageRepositoryUpdater : IRepositoryUpdater
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<RsiRepositoryUpdater>();

        private readonly IRepository<MeanAverageStorageObject> m_repository;
        private readonly string m_currency;
        private readonly int m_meanAverageSize;
        private readonly string m_calculatedDataFolder;

        public MeanAverageRepositoryUpdater(IRepository<MeanAverageStorageObject> repository, 
            string currency, 
            int meanAverageSize,
            string calculatedDataFolder)
        {
            m_repository = repository;
            m_currency = currency;
            m_meanAverageSize = meanAverageSize;
            m_calculatedDataFolder = calculatedDataFolder;
        }

        public void AddInfo(CandleStorageObject candle, DateTime newTime)
        {
            if (m_repository.TryGet(m_currency, newTime, out _))
            {
                return;
            }
            
            AddMeanAverageToRepository(candle, newTime);
        }

        public async Task PersistDataToFileAsync()
        {
            string storageObjectsFileName =
                CalculatedFileProvider.GetCalculatedMeanAverageFile(m_currency, m_meanAverageSize, m_calculatedDataFolder);
            await m_repository.SaveDataToFileAsync(m_currency, storageObjectsFileName);
        }
        
        private void AddMeanAverageToRepository(CandleStorageObject candle, DateTime newTime)
        {
            decimal newMeanAverageValue = CalculateMeanAverage(candle, newTime);
            var newMeanAverage = new MeanAverageStorageObject(newMeanAverageValue, newTime);
            m_repository.Add(m_currency, newTime, newMeanAverage);
        }

        private decimal CalculateMeanAverage(CandleStorageObject candle, DateTime newTime)
        {
            DateTime previousMeanAverageTime = newTime.Subtract(candle.Candle.CandleSizeInMinutes);
            decimal currentPrice = candle.Candle.Close;
            if (m_repository.TryGet(m_currency, previousMeanAverageTime,
                out MeanAverageStorageObject previousMeanAverageStorageObject))
            {
                return (previousMeanAverageStorageObject.MeanAverage * (m_meanAverageSize - 1) + currentPrice) /
                       m_meanAverageSize;
            }

            s_logger.LogInformation($"{m_currency}: CalculateFirstMeanAverage {previousMeanAverageTime:dd/MM/yyyy HH:mm:ss}");
            return currentPrice;
        }
    }
}