using System;
using Common;
using Common.DataStorageObjects;
using Infra;
using Microsoft.Extensions.Logging;
using Storage.Abstractions;
using Storage.Repository;
using Utils.Calculators;

namespace Storage.Updaters
{
    public class RsiRepositoryUpdater : IRepositoryUpdater
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<RsiRepositoryUpdater>();

        private readonly RepositoryImpl<RsiStorageObject> m_rsiRepository;
        private readonly RepositoryImpl<WsmaStorageObject> m_wsmaRepository;
        private readonly string m_symbol;
        private readonly int m_rsiSize;

        public RsiRepositoryUpdater(RepositoryImpl<RsiStorageObject> rsiRepository, 
            RepositoryImpl<WsmaStorageObject> wsmaRepository, string symbol, int rsiSize)
        {
            m_rsiRepository = rsiRepository;
            m_symbol = symbol;
            m_wsmaRepository = wsmaRepository;
            m_rsiSize = rsiSize;
        }

        public void AddInfo(MyCandle candle, DateTime previousTime, DateTime newTime)
        {
            AddWsmaToRepository(candle, previousTime, newTime);
            AddRsiToRepository(newTime);
        }

        private void AddRsiToRepository(DateTime newTime)
        {
            decimal newRsi = CalculateNewRsi(newTime);
            RsiStorageObject rsiStorageObject = new RsiStorageObject(newRsi);
            m_rsiRepository.Add(m_symbol, newTime, rsiStorageObject);
        }

        private void AddWsmaToRepository(MyCandle candle, DateTime previousWsmTime, DateTime newWsmaTime)
        {
            WsmaStorageObject newWsma = CalculateNewWsma(previousWsmTime, candle);
            m_wsmaRepository.Add(m_symbol, newWsmaTime, newWsma);
        }

        private decimal CalculateNewRsi(DateTime newRsiTime)
        {
            WsmaStorageObject wsma = m_wsmaRepository.Get(m_symbol, newRsiTime);
            return RsiCalculator.Calculate(wsma.UpAverage, wsma.DownAverage);
        }

        private WsmaStorageObject CalculateNewWsma(DateTime previousWsmaTime, MyCandle candle)
        {
            (decimal upValue, decimal downValue) = GetLastCandleUpAndDownValue(candle);
            if (m_wsmaRepository.TryGet(m_symbol,previousWsmaTime, out WsmaStorageObject previousWsma))
            {
                return CalculateWsmaUsingPreviousWsma(upValue, previousWsma, downValue);
            }

            s_logger.LogInformation($"{m_symbol}: CalculateFirstWsma {previousWsmaTime}");
            return CalculateFirstWsma(candle);
        }

        private WsmaStorageObject CalculateWsmaUsingPreviousWsma(decimal upValue, WsmaStorageObject previousWsma,
            decimal downValue)
        {
            decimal newUpAvg = WsmaCalculator.Calculate(upValue, previousWsma.UpAverage, m_rsiSize);
            decimal newDownAvg = WsmaCalculator.Calculate(downValue, previousWsma.DownAverage, m_rsiSize);
            return new WsmaStorageObject(newUpAvg, newDownAvg);
        }

        private static WsmaStorageObject CalculateFirstWsma(MyCandle candle)
        {
            var gainOrLossAmount = (candle.Close - candle.Open);
            return gainOrLossAmount > 0 ? 
                new WsmaStorageObject(gainOrLossAmount, gainOrLossAmount) : 
                new WsmaStorageObject(-gainOrLossAmount, -gainOrLossAmount);
        }

        private static (decimal upValue, decimal downValue) GetLastCandleUpAndDownValue(MyCandle candle)
        {
            decimal currentCandleDiff = candle.Close - candle.Open;
            
            decimal upValue, downValue;
            if (currentCandleDiff > 0)
            {
                upValue = currentCandleDiff;
                downValue = 0;
            }
            else
            {
                upValue = 0;
                downValue = -currentCandleDiff;
            }

            return (upValue, downValue);
        }
    }
}