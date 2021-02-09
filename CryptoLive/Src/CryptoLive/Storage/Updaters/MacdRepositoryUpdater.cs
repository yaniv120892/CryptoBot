using System;
using System.Threading.Tasks;
using Common.DataStorageObjects;
using Infra;
using Microsoft.Extensions.Logging;
using Storage.Abstractions;
using Utils.Calculators;

namespace Storage.Updaters
{
    public class MacdRepositoryUpdater : IRepositoryUpdater
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<MacdRepositoryUpdater>();

        private readonly IRepository<MacdStorageObject> m_macdRepository;
        private readonly IRepository<EmaAndSignalStorageObject> m_emaAndSignalStorageObject;
        private readonly string m_symbol;
        private readonly int m_fastEmaSize;
        private readonly int m_slowEmaSize;
        private readonly int m_signalSize;
        private readonly string m_calculatedDataFolder;

        public MacdRepositoryUpdater(IRepository<MacdStorageObject> macdRepository, 
            IRepository<EmaAndSignalStorageObject> emaAndSignalStorageObject, 
            string symbol,
            int fastEmaSize, 
            int slowEmaSize,
            int signalSize, 
            string calculatedDataFolder)
        {
            m_macdRepository = macdRepository;
            m_symbol = symbol;
            m_fastEmaSize = fastEmaSize;
            m_slowEmaSize = slowEmaSize;
            m_signalSize = signalSize;
            m_calculatedDataFolder = calculatedDataFolder;
            m_emaAndSignalStorageObject = emaAndSignalStorageObject;
        }

        public void AddInfo(CandleStorageObject candle, DateTime previousTime, DateTime newTime)
        {
            AddEmaAndSignalToRepository(candle, previousTime, newTime);
            AddMacdToRepository(newTime);
        }

        private void AddMacdToRepository(DateTime newTime)
        {
            decimal newMacdHistogram = CalculateNewMacdHistogram(newTime);
            MacdStorageObject macdStorageObject = new MacdStorageObject(newMacdHistogram, newTime);
            m_macdRepository.Add(m_symbol, newTime, macdStorageObject);
        }

        private void AddEmaAndSignalToRepository(CandleStorageObject candle, DateTime previousMacdTime, DateTime newMacdTime)
        {
            EmaAndSignalStorageObject emaAndSignalStorageObject = CalculateNewEmaAndSignal(previousMacdTime, candle, newMacdTime);
            m_emaAndSignalStorageObject.Add(m_symbol, newMacdTime, emaAndSignalStorageObject);        
        }

        private EmaAndSignalStorageObject CalculateNewEmaAndSignal(DateTime previousMacdTime, CandleStorageObject candle,
            DateTime newTime)
        {
            if (m_emaAndSignalStorageObject.TryGet(m_symbol, previousMacdTime, out EmaAndSignalStorageObject previousEmaAndSignalStorageObject))
            {
                return CalculateEmaAndSignalUsingPreviousEmaAndSignal(previousEmaAndSignalStorageObject, candle.Candle.Close, newTime);
            }

            s_logger.LogInformation($"{m_symbol}: CalculateFirstEmaAndSignal {previousMacdTime}");
            return CalculateFirstEmaAndSignal(candle, newTime);

        }

        private static EmaAndSignalStorageObject CalculateFirstEmaAndSignal(CandleStorageObject candle, DateTime newTime)
        {
            return new EmaAndSignalStorageObject(candle.Candle.Close, candle.Candle.Close, 0, newTime);
        }

        private EmaAndSignalStorageObject CalculateEmaAndSignalUsingPreviousEmaAndSignal(EmaAndSignalStorageObject previousEmaAndSignalStorageObject, 
            decimal candleClose, DateTime newTime)
        {
            decimal newFastEma = EmaCalculator.Calculate(candleClose, previousEmaAndSignalStorageObject.FastEma, m_fastEmaSize);
            decimal newSlowEma = EmaCalculator.Calculate(candleClose, previousEmaAndSignalStorageObject.SlowEma, m_slowEmaSize);
            decimal newDiff = newFastEma - newSlowEma;
            decimal newSignal = EmaCalculator.Calculate(newDiff, previousEmaAndSignalStorageObject.Signal, m_signalSize);
            return new EmaAndSignalStorageObject(newFastEma, newSlowEma, newSignal, newTime);
        }

        private decimal CalculateNewMacdHistogram(DateTime newMacdTime)
        {
            EmaAndSignalStorageObject emaAndSignalStorageObject = m_emaAndSignalStorageObject.Get(m_symbol, newMacdTime);
            return MacdHistogramCalculator.Calculate(emaAndSignalStorageObject.FastEma, emaAndSignalStorageObject.SlowEma, 
                emaAndSignalStorageObject.Signal);
        }
        
        public async Task PersistDataToFileAsync()
        {
            string macdStorageObjectFileName = 
                CalculatedFileProvider.GetCalculatedMacdFile(m_symbol, 
                    m_slowEmaSize, m_fastEmaSize,
                    m_signalSize, m_calculatedDataFolder);
            await m_macdRepository.SaveDataToFileAsync(m_symbol, macdStorageObjectFileName);
            string emaAndSignalStorageObjectFileName = CalculatedFileProvider.GetCalculatedEmaAndSignalFile(m_symbol,
                m_slowEmaSize, m_fastEmaSize,
                m_signalSize, m_calculatedDataFolder);
            await m_emaAndSignalStorageObject.SaveDataToFileAsync(m_symbol, emaAndSignalStorageObjectFileName);
        }
    }
}