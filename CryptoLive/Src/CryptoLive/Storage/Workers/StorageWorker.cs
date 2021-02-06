using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Infra;
using Microsoft.Extensions.Logging;
using Services.Abstractions;
using Storage.Abstractions;
using Utils.Abstractions;
using Utils.Converters;

namespace Storage.Workers
{
    public class StorageWorker
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<StorageWorker>();

        private readonly ISystemClock m_systemClock;
        private readonly ICandlesService m_candlesService;
        private readonly StorageWorkerMode m_storageWorkerMode;
        private readonly IRepositoryUpdater m_rsiRepositoryUpdater;
        private readonly IRepositoryUpdater m_candleRepositoryUpdater;
        private readonly IRepositoryUpdater m_macdRepositoryUpdater;
        private readonly CancellationToken m_cancellationToken;
        private readonly int m_candleSize;
        private readonly string m_symbol;

        public StorageWorker(
            ICandlesService candlesService,
            ISystemClock systemClock,
            IRepositoryUpdater rsiRepositoryUpdater, 
            IRepositoryUpdater candleRepositoryUpdater, 
            IRepositoryUpdater macdRepositoryUpdater,
            CancellationToken cancellationToken,
            int candleSize,
            string symbol,
            StorageWorkerMode storageWorkerMode)
        {
            m_systemClock = systemClock;
            m_candleSize = candleSize;
            m_symbol = symbol;
            m_storageWorkerMode = storageWorkerMode;
            m_rsiRepositoryUpdater = rsiRepositoryUpdater;
            m_candleRepositoryUpdater = candleRepositoryUpdater;
            m_macdRepositoryUpdater = macdRepositoryUpdater;
            m_candlesService = candlesService;
            m_cancellationToken = cancellationToken;
        }

        public async Task StartAsync(DateTime currentTime)
        {
            s_logger.LogInformation($"Start {nameof(StorageWorker)} for {m_symbol} at {currentTime}");
            Stopwatch stopwatch = new Stopwatch();
            try
            {
                while (!m_cancellationToken.IsCancellationRequested)
                {

                    stopwatch.Restart();
                    await AddDataToRepositories(currentTime);
                    stopwatch.Stop();

                    int timeToWaitInSeconds = GetTimeToWait(stopwatch.Elapsed.Seconds);
                    if (timeToWaitInSeconds < 0)
                    {
                        s_logger.LogWarning($"Add data to repositories took way long {stopwatch.Elapsed.Seconds}");
                        break;
                    }


                    currentTime = await m_systemClock.Wait(m_cancellationToken, m_symbol, timeToWaitInSeconds,
                        nameof(StorageWorker), currentTime);
                }
            }
            catch (Exception e)
            {
                s_logger.LogError(e, $"{m_symbol} {nameof(StorageWorker)} got exception {currentTime}");
            }

            s_logger.LogInformation($"Stop {nameof(StorageWorker)} for {m_symbol}");
        }

        private int GetTimeToWait(int elapsedSeconds)
        {
            if (m_storageWorkerMode.Equals(StorageWorkerMode.Live))
            {
                return 60 - elapsedSeconds;
            }
            return 60;
        }
        
        private (DateTime previousTime, DateTime newTime) GetNewAndPreviousCandleTimes(MyCandle candleDescription)
        {
            DateTime newTime = candleDescription.CloseTime;
            DateTime newDateTimeWithoutSeconds = newTime.AddSeconds(-newTime.Second);
            DateTime previousTime = newDateTimeWithoutSeconds.Subtract(TimeSpan.FromMinutes(m_candleSize));

            return (previousTime, newDateTimeWithoutSeconds);
        } 

        private async Task AddDataToRepositories(DateTime currentTime)
        {
            int amountOfOneMinuteKlines = m_candleSize + 1; // +1 in order to ignore last candle that didn't finish yet
            Memory<MyCandle> oneMinuteCandlesDescription = await m_candlesService.GetOneMinuteCandles(m_symbol, amountOfOneMinuteKlines, currentTime);
            MyCandle candle = BinanceKlineToMyCandleConverter.ConvertByCandleSize(oneMinuteCandlesDescription.Span, m_candleSize);
            (DateTime previousCandleTime, DateTime newCandleTime)  = GetNewAndPreviousCandleTimes(candle);
            m_candleRepositoryUpdater.AddInfo(candle, previousCandleTime, newCandleTime);
            m_rsiRepositoryUpdater.AddInfo(candle, previousCandleTime, newCandleTime);
            m_macdRepositoryUpdater.AddInfo(candle, previousCandleTime, newCandleTime);
        }
    }
}