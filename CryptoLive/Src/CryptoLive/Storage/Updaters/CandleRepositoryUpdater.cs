using System;
using System.Threading.Tasks;
using Common.DataStorageObjects;
using Storage.Abstractions;

namespace Storage.Updaters
{
    public class CandleRepositoryUpdater : IRepositoryUpdater
    {
        private readonly IRepository<CandleStorageObject> m_candleRepository;
        private readonly string m_symbol;
        private readonly int m_candleSize;
        private readonly string m_calculatedDataFolder;

        public CandleRepositoryUpdater(IRepository<CandleStorageObject> candleRepository, 
            string symbol, 
            int candleSize, 
            string calculatedDataFolder)
        {
            m_candleRepository = candleRepository;
            m_symbol = symbol;
            m_candleSize = candleSize;
            m_calculatedDataFolder = calculatedDataFolder;
        }

        public void AddInfo(CandleStorageObject candle, DateTime previousTime, DateTime newTime)
        {
            m_candleRepository.Add(m_symbol, newTime, candle);
        }
        
        public async Task PersistDataToFileAsync()
        {
            string rsiStorageObjectsFileName = CalculatedFileProvider.GetCalculatedCandleFile(m_symbol, m_candleSize, m_calculatedDataFolder);
            await m_candleRepository.SaveDataToFileAsync(m_symbol, rsiStorageObjectsFileName);
        }
    }
}