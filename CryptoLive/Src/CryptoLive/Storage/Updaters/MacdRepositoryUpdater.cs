using System;
using System.Threading.Tasks;
using Common.DataStorageObjects;
using Infra;
using Microsoft.Extensions.Logging;
using Storage.Abstractions.Repository;
using Utils.Calculators;

namespace Storage.Updaters
{
    public class MacdRepositoryUpdater : IRepositoryUpdater
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<MacdRepositoryUpdater>();

        private readonly IRepository<MacdStorageObject> m_macdRepository;
        private readonly IRepository<EmaAndSignalStorageObject> m_emaAndSignalStorageObject;
        private readonly string m_currency;
        private readonly int m_fastEmaSize;
        private readonly int m_slowEmaSize;
        private readonly int m_signalSize;
        private readonly string m_calculatedDataFolder;

        public MacdRepositoryUpdater(IRepository<MacdStorageObject> macdRepository, 
            IRepository<EmaAndSignalStorageObject> emaAndSignalStorageObject, 
            string currency,
            int fastEmaSize, 
            int slowEmaSize,
            int signalSize, 
            string calculatedDataFolder)
        {
            m_macdRepository = macdRepository;
            m_currency = currency;
            m_fastEmaSize = fastEmaSize;
            m_slowEmaSize = slowEmaSize;
            m_signalSize = signalSize;
            m_calculatedDataFolder = calculatedDataFolder;
            m_emaAndSignalStorageObject = emaAndSignalStorageObject;
        }

        public void AddInfo(CandleStorageObject candle, DateTime previousTime, DateTime newTime)
        {
            if (m_macdRepository.TryGet(m_currency, newTime, out _))
            {
                return;
            }
            AddEmaAndSignalToRepository(candle, previousTime, newTime);
            AddMacdToRepository(newTime);
        }

        private void AddMacdToRepository(DateTime newTime)
        {
            decimal newMacdHistogram = CalculateNewMacdHistogram(newTime);
            MacdStorageObject macdStorageObject = new MacdStorageObject(newMacdHistogram, newTime);
            m_macdRepository.Add(m_currency, newTime, macdStorageObject);
        }

        private void AddEmaAndSignalToRepository(CandleStorageObject candle, DateTime previousMacdTime, DateTime newMacdTime)
        {
            EmaAndSignalStorageObject emaAndSignalStorageObject = CalculateNewEmaAndSignal(previousMacdTime, candle, newMacdTime);
            m_emaAndSignalStorageObject.Add(m_currency, newMacdTime, emaAndSignalStorageObject);        
        }

        private EmaAndSignalStorageObject CalculateNewEmaAndSignal(DateTime previousMacdTime, CandleStorageObject candle,
            DateTime newTime)
        {
            if (m_emaAndSignalStorageObject.TryGet(m_currency, previousMacdTime, out EmaAndSignalStorageObject previousEmaAndSignalStorageObject))
            {
                return CalculateEmaAndSignalUsingPreviousEmaAndSignal(previousEmaAndSignalStorageObject, candle.Candle.Close, newTime);
            }

            s_logger.LogInformation($"{m_currency}: CalculateFirstEmaAndSignal {previousMacdTime}");
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
            EmaAndSignalStorageObject emaAndSignalStorageObject = m_emaAndSignalStorageObject.Get(m_currency, newMacdTime);
            return MacdHistogramCalculator.Calculate(emaAndSignalStorageObject.FastEma, emaAndSignalStorageObject.SlowEma, 
                emaAndSignalStorageObject.Signal);
        }
        
        public async Task PersistDataToFileAsync()
        {
            string macdStorageObjectFileName = 
                CalculatedFileProvider.GetCalculatedMacdFile(m_currency, 
                    m_slowEmaSize, m_fastEmaSize,
                    m_signalSize, m_calculatedDataFolder);
            await m_macdRepository.SaveDataToFileAsync(m_currency, macdStorageObjectFileName);
            string emaAndSignalStorageObjectFileName = CalculatedFileProvider.GetCalculatedEmaAndSignalFile(m_currency,
                m_slowEmaSize, m_fastEmaSize,
                m_signalSize, m_calculatedDataFolder);
            await m_emaAndSignalStorageObject.SaveDataToFileAsync(m_currency, emaAndSignalStorageObjectFileName);
        }
    }
}