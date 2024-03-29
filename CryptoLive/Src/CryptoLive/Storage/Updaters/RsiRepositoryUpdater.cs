using System;
using System.Threading.Tasks;
using Common.DataStorageObjects;
using Infra;
using Microsoft.Extensions.Logging;
using Storage.Abstractions.Repository;
using Utils.Calculators;

namespace Storage.Updaters
{
    public class RsiRepositoryUpdater : IRepositoryUpdater
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<RsiRepositoryUpdater>();

        private readonly IRepository<RsiStorageObject> m_rsiRepository;
        private readonly IRepository<WsmaStorageObject> m_wsmaRepository;
        private readonly string m_currency;
        private readonly int m_rsiSize;
        private readonly string m_calculatedDataFolder;

        private bool m_addedNewData;

        public RsiRepositoryUpdater(IRepository<RsiStorageObject> rsiRepository, 
            IRepository<WsmaStorageObject> wsmaRepository, string currency, int rsiSize,
            string calculatedDataFolder)
        {
            m_rsiRepository = rsiRepository;
            m_currency = currency;
            m_wsmaRepository = wsmaRepository;
            m_rsiSize = rsiSize;
            m_calculatedDataFolder = calculatedDataFolder;
        }

        public void AddInfo(CandleStorageObject candle, DateTime newTime)
        {
            if (m_rsiRepository.TryGet(m_currency, newTime, out _))
            {
                return;
            }

            m_addedNewData = true;
            AddWsmaToRepository(candle, newTime);
            AddRsiToRepository(newTime);
        }

        public async Task PersistDataToFileAsync()
        {
            if (m_addedNewData)
            {
                string rsiStorageObjectsFileName =
                    CalculatedFileProvider.GetCalculatedRsiFile(m_currency, m_rsiSize, m_calculatedDataFolder);
                await m_rsiRepository.SaveDataToFileAsync(m_currency, rsiStorageObjectsFileName);
                string wsmaStorageObjectsFileName = 
                    CalculatedFileProvider.GetCalculatedWsmaFile(m_currency, m_rsiSize, m_calculatedDataFolder);
                await m_wsmaRepository.SaveDataToFileAsync(m_currency, wsmaStorageObjectsFileName);
            }
        }

        private void AddRsiToRepository(DateTime newTime)
        {
            decimal newRsi = CalculateNewRsi(newTime);
            RsiStorageObject rsiStorageObject = new RsiStorageObject(newRsi, newTime);
            m_rsiRepository.Add(m_currency, newTime, rsiStorageObject);
        }

        private void AddWsmaToRepository(CandleStorageObject candle, DateTime newWsmaTime)
        {
            WsmaStorageObject newWsma = CalculateNewWsma(candle, newWsmaTime);
            m_wsmaRepository.Add(m_currency, newWsmaTime, newWsma);
        }

        private decimal CalculateNewRsi(DateTime newRsiTime)
        {
            WsmaStorageObject wsma = m_wsmaRepository.Get(m_currency, newRsiTime);
            return RsiCalculator.Calculate(wsma.UpAverage, wsma.DownAverage);
        }

        private WsmaStorageObject CalculateNewWsma(CandleStorageObject candle, DateTime newTime)
        {
            DateTime previousWsmaTime = newTime.Subtract(candle.Candle.CandleSizeInMinutes);
            (decimal upValue, decimal downValue) = GetLastCandleUpAndDownValue(candle);
            if (m_wsmaRepository.TryGet(m_currency,previousWsmaTime, out WsmaStorageObject previousWsma))
            {
                return CalculateWsmaUsingPreviousWsma(upValue, previousWsma, downValue, newTime);
            }

            s_logger.LogInformation($"{m_currency}: CalculateFirstWsma {previousWsmaTime:dd/MM/yyyy HH:mm:ss}");
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