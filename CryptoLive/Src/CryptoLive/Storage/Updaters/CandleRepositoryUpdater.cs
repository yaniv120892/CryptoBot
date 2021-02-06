using System;
using Common;
using Storage.Abstractions;

namespace Storage.Updaters
{
    public class CandleRepositoryUpdater : IRepositoryUpdater
    {
        private readonly IRepository<MyCandle> m_candleRepository;
        private readonly string m_symbol;

        public CandleRepositoryUpdater(IRepository<MyCandle> candleRepository, string symbol)
        {
            m_candleRepository = candleRepository;
            m_symbol = symbol;
        }

        public void AddInfo(MyCandle candle, DateTime previousTime, DateTime newTime)
        {
            m_candleRepository.Add(m_symbol, newTime, candle);
        }
    }
}