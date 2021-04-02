using System;
using System.Threading.Tasks;
using Common.DataStorageObjects;
using Storage.Abstractions.Repository;

namespace Storage.Updaters
{
    public class CandleRepositoryUpdater : IRepositoryUpdater
    {
        private readonly IRepository<CandleStorageObject> m_candleRepository;
        private readonly string m_currency;
        private readonly string m_calculatedDataFolder;

        public CandleRepositoryUpdater(IRepository<CandleStorageObject> candleRepository, 
            string currency, 
            string calculatedDataFolder)
        {
            m_candleRepository = candleRepository;
            m_currency = currency;
            m_calculatedDataFolder = calculatedDataFolder;
        }

        public void AddInfo(CandleStorageObject candle, DateTime newTime)
        {
            m_candleRepository.Add(m_currency, newTime, candle);
        }
        
        public async Task PersistDataToFileAsync()
        {
            string candleStorageObjectsFileName = CalculatedFileProvider.GetCalculatedCandleFile(m_currency, 1, m_calculatedDataFolder);
            await m_candleRepository.SaveDataToFileAsync(m_currency, candleStorageObjectsFileName);
        }
    }
}