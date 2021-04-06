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

        private readonly IRepository<MeanAverageStorageObject> m_meanAverageRepository;
        private readonly IRepository<CandleStorageObject> m_candleRepository;
        private readonly string m_currency;
        private readonly int m_meanAverageSize;
        private readonly string m_calculatedDataFolder;

        private bool m_addedNewData;

        public MeanAverageRepositoryUpdater(IRepository<MeanAverageStorageObject> meanAverageRepository,
            IRepository<CandleStorageObject> candleRepository,
            string currency, 
            int meanAverageSize,
            string calculatedDataFolder)
        {
            m_meanAverageRepository = meanAverageRepository;
            m_currency = currency;
            m_meanAverageSize = meanAverageSize;
            m_calculatedDataFolder = calculatedDataFolder;
            m_candleRepository = candleRepository;
        }

        public void AddInfo(CandleStorageObject candle, DateTime newTime)
        {
            if (m_meanAverageRepository.TryGet(m_currency, newTime, out _))
            {
                return;
            }
            m_addedNewData = true;
            AddMeanAverageToRepository(candle, newTime);
        }

        public async Task PersistDataToFileAsync()
        {
            if (m_addedNewData)
            {
                string storageObjectsFileName =
                    CalculatedFileProvider.GetCalculatedMeanAverageFile(m_currency, m_meanAverageSize, m_calculatedDataFolder);
                await m_meanAverageRepository.SaveDataToFileAsync(m_currency, storageObjectsFileName);
            }
        }
        
        private void AddMeanAverageToRepository(CandleStorageObject candle, DateTime newTime)
        {
            decimal newMeanAverageValue = CalculateMeanAverage(candle, newTime);
            var newMeanAverage = new MeanAverageStorageObject(newMeanAverageValue, newTime);
            m_meanAverageRepository.Add(m_currency, newTime, newMeanAverage);
        }

        private decimal CalculateMeanAverage(CandleStorageObject candle, DateTime newTime)
        {
            int candleSize = candle.Candle.CandleSizeInMinutes.Minutes;
            decimal currentPrice = candle.Candle.Close;
            DateTime timeOfPriceToRemoveFromAverage = newTime.Subtract(TimeSpan.FromMinutes(candleSize * m_meanAverageSize));
            if (m_candleRepository.TryGet(m_currency, timeOfPriceToRemoveFromAverage,
                out CandleStorageObject candleToRemoveFromAverage))
            {
                DateTime previousMeanAverageTime = newTime.Subtract(TimeSpan.FromMinutes(candleSize));
                if (m_meanAverageRepository.TryGet(m_currency, previousMeanAverageTime,
                    out MeanAverageStorageObject previousMeanAverageStorageObject))
                {
                    decimal priceToRemoveFromAverage = candleToRemoveFromAverage.Candle.Close;
                    return (previousMeanAverageStorageObject.MeanAverage * m_meanAverageSize - priceToRemoveFromAverage + currentPrice) /
                           m_meanAverageSize;
                }
            }

            s_logger.LogInformation($"{m_currency}: CalculateFirstMeanAverage {newTime:dd/MM/yyyy HH:mm:ss}");
            return currentPrice;
        }
    }
}