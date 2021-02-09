using System;
using System.Threading.Tasks;
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
        private readonly string m_calculatedDataFolder;

        public RsiRepositoryUpdater(RepositoryImpl<RsiStorageObject> rsiRepository, 
            RepositoryImpl<WsmaStorageObject> wsmaRepository, string symbol, int rsiSize,
            string calculatedDataFolder)
        {
            m_rsiRepository = rsiRepository;
            m_symbol = symbol;
            m_wsmaRepository = wsmaRepository;
            m_rsiSize = rsiSize;
            m_calculatedDataFolder = calculatedDataFolder;
        }

        public void AddInfo(CandleStorageObject candle, DateTime previousTime, DateTime newTime)
        {
            AddWsmaToRepository(candle, previousTime, newTime);
            AddRsiToRepository(newTime);
        }

        public async Task PersistDataToFileAsync()
        {
            string rsiStorageObjectsFileName =
                CalculatedFileProvider.GetCalculatedRsiFile(m_symbol, m_rsiSize, m_calculatedDataFolder);
            await m_rsiRepository.SaveDataToFileAsync(m_symbol, rsiStorageObjectsFileName);
            string wsmaStorageObjectsFileName = 
                CalculatedFileProvider.GetCalculatedWsmaFile(m_symbol, m_rsiSize, m_calculatedDataFolder);
            await m_wsmaRepository.SaveDataToFileAsync(m_symbol, wsmaStorageObjectsFileName);
        }

        private void AddRsiToRepository(DateTime newTime)
        {
            decimal newRsi = CalculateNewRsi(newTime);
            RsiStorageObject rsiStorageObject = new RsiStorageObject(newRsi, newTime);
            m_rsiRepository.Add(m_symbol, newTime, rsiStorageObject);
        }

        private void AddWsmaToRepository(CandleStorageObject candle, DateTime previousWsmTime, DateTime newWsmaTime)
        {
            WsmaStorageObject newWsma = CalculateNewWsma(previousWsmTime, candle, newWsmaTime);
            m_wsmaRepository.Add(m_symbol, newWsmaTime, newWsma);
        }

        private decimal CalculateNewRsi(DateTime newRsiTime)
        {
            WsmaStorageObject wsma = m_wsmaRepository.Get(m_symbol, newRsiTime);
            return RsiCalculator.Calculate(wsma.UpAverage, wsma.DownAverage);
        }

        private WsmaStorageObject CalculateNewWsma(DateTime previousWsmaTime, CandleStorageObject candle, DateTime newTime)
        {
            (decimal upValue, decimal downValue) = GetLastCandleUpAndDownValue(candle);
            if (m_wsmaRepository.TryGet(m_symbol,previousWsmaTime, out WsmaStorageObject previousWsma))
            {
                return CalculateWsmaUsingPreviousWsma(upValue, previousWsma, downValue, newTime);
            }

            s_logger.LogInformation($"{m_symbol}: CalculateFirstWsma {previousWsmaTime}");
            return CalculateFirstWsma(candle, newTime);
        }

        private WsmaStorageObject CalculateWsmaUsingPreviousWsma(decimal upValue, WsmaStorageObject previousWsma,
            decimal downValue, DateTime newTime)
        {
            decimal newUpAvg = WsmaCalculator.Calculate(upValue, previousWsma.UpAverage, m_rsiSize);
            decimal newDownAvg = WsmaCalculator.Calculate(downValue, previousWsma.DownAverage, m_rsiSize);
            return new WsmaStorageObject(newUpAvg, newDownAvg, newTime);
        }

        private static WsmaStorageObject CalculateFirstWsma(CandleStorageObject candle, DateTime newTime)
        {
            var winOrLossAmount = (candle.Candle.Close - candle.Candle.Open);
            return winOrLossAmount > 0 ? 
                new WsmaStorageObject(winOrLossAmount, winOrLossAmount, newTime) : 
                new WsmaStorageObject(-winOrLossAmount, -winOrLossAmount, newTime);
        }

        private static (decimal upValue, decimal downValue) GetLastCandleUpAndDownValue(CandleStorageObject candle)
        {
            decimal currentCandleDiff = candle.Candle.Close - candle.Candle.Open;
            
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