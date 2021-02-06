using System;
using Common;
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

        public MacdRepositoryUpdater(IRepository<MacdStorageObject> macdRepository, 
            IRepository<EmaAndSignalStorageObject> emaAndSignalStorageObject, 
            string symbol,
            int fastEmaSize, 
            int slowEmaSize,
            int signalSize)
        {
            m_macdRepository = macdRepository;
            m_symbol = symbol;
            m_fastEmaSize = fastEmaSize;
            m_slowEmaSize = slowEmaSize;
            m_signalSize = signalSize;
            m_emaAndSignalStorageObject = emaAndSignalStorageObject;
        }

        public void AddInfo(MyCandle candle, DateTime previousTime, DateTime newTime)
        {
            AddEmaAndSignalToRepository(candle, previousTime, newTime);
            AddMacdToRepository(newTime);
        }

        private void AddMacdToRepository(DateTime newTime)
        {
            decimal newMacdHistogram = CalculateNewMacdHistogram(newTime);
            MacdStorageObject macdStorageObject = new MacdStorageObject(newMacdHistogram);
            m_macdRepository.Add(m_symbol, newTime, macdStorageObject);
        }

        private void AddEmaAndSignalToRepository(MyCandle candle, DateTime previousMacdTime, DateTime newMacdTime)
        {
            EmaAndSignalStorageObject emaAndSignalStorageObject = CalculateNewEmaAndSignal(previousMacdTime, candle);
            m_emaAndSignalStorageObject.Add(m_symbol, newMacdTime, emaAndSignalStorageObject);        
        }

        private EmaAndSignalStorageObject CalculateNewEmaAndSignal(DateTime previousMacdTime, MyCandle candle)
        {
            if (m_emaAndSignalStorageObject.TryGet(m_symbol, previousMacdTime, out EmaAndSignalStorageObject previousEmaAndSignalStorageObject))
            {
                return CalculateEmaAndSignalUsingPreviousEmaAndSignal(previousEmaAndSignalStorageObject, candle.Close);
            }

            s_logger.LogInformation($"{m_symbol}: CalculateFirstEmaAndSignal {previousMacdTime}");
            return CalculateFirstEmaAndSignal(candle);
            
        }

        private static EmaAndSignalStorageObject CalculateFirstEmaAndSignal(MyCandle candle)
        {
            return new EmaAndSignalStorageObject(candle.Close, candle.Close, 0);
        }

        private EmaAndSignalStorageObject CalculateEmaAndSignalUsingPreviousEmaAndSignal(EmaAndSignalStorageObject previousEmaAndSignalStorageObject, decimal candleClose)
        {
            decimal newFastEma = EmaCalculator.Calculate(candleClose, previousEmaAndSignalStorageObject.FastEma, m_fastEmaSize);
            decimal newSlowEma = EmaCalculator.Calculate(candleClose, previousEmaAndSignalStorageObject.SlowEma, m_slowEmaSize);
            decimal newDiff = newFastEma - newSlowEma;
            decimal newSignal = EmaCalculator.Calculate(newDiff, previousEmaAndSignalStorageObject.Signal, m_signalSize);
            return new EmaAndSignalStorageObject(newFastEma, newSlowEma, newSignal);
        }

        private decimal CalculateNewMacdHistogram(DateTime newMacdTime)
        {
            EmaAndSignalStorageObject emaAndSignalStorageObject = m_emaAndSignalStorageObject.Get(m_symbol, newMacdTime);
            return MacdHistogramCalculator.Calculate(emaAndSignalStorageObject.FastEma, emaAndSignalStorageObject.SlowEma, 
                emaAndSignalStorageObject.Signal);
        }
    }
}