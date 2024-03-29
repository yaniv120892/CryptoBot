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
        
        private bool m_addedNewData;

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
            if (m_candleRepository.TryGet(m_currency, newTime, out _))
            {
                return;
            }
            m_addedNewData = true;
            m_candleRepository.Add(m_currency, newTime, candle);
        }
        
        public async Task PersistDataToFileAsync()
        {
            if (m_addedNewData)
            {
                string candleStorageObjectsFileName = CalculatedFileProvider.GetCalculatedCandleFile(m_currency, m_calculatedDataFolder);
                await m_candleRepository.SaveDataToFileAsync(m_currency, candleStorageObjectsFileName);
            }
        }
    }
}