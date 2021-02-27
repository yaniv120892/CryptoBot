using System;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.DataStorageObjects;
using Infra;
using Microsoft.Extensions.Logging;
using Services.Abstractions;
using Storage.Abstractions.Repository;
using Utils.Abstractions;
using Utils.Converters;

namespace Storage.Workers
{
    public class StorageWorker
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<StorageWorker>();

        private readonly INotificationService m_notificationService;
        private readonly ISystemClock m_systemClock;
        private readonly IStopWatch m_stopWatch;
        private readonly ICandlesService m_candlesService;
        private readonly IRepositoryUpdater m_rsiRepositoryUpdater;
        private readonly IRepositoryUpdater m_candleRepositoryUpdater;
        private readonly IRepositoryUpdater m_macdRepositoryUpdater;
        private readonly CancellationToken m_cancellationToken;
        private readonly int m_candleSize;
        private readonly string m_currency;
        private readonly bool m_persistData;
        private readonly int m_iterationTimeInSeconds;
        private DateTime m_currentTime;

        public WorkerStatus WorkerStatus { get; private set; }

        public StorageWorker(
            INotificationService notificationService,
            ICandlesService candlesService,
            ISystemClock systemClock,
            IStopWatch stopWatch,
            IRepositoryUpdater rsiRepositoryUpdater, 
            IRepositoryUpdater candleRepositoryUpdater, 
            IRepositoryUpdater macdRepositoryUpdater,
            CancellationToken cancellationToken,
            int candleSize,
            string currency,
            bool persistData,
            int iterationTimeInSeconds)
        {
            m_notificationService = notificationService;
            m_systemClock = systemClock;
            m_stopWatch = stopWatch;
            m_candleSize = candleSize;
            m_currency = currency;
            m_persistData = persistData;
            m_iterationTimeInSeconds = iterationTimeInSeconds;
            m_rsiRepositoryUpdater = rsiRepositoryUpdater;
            m_candleRepositoryUpdater = candleRepositoryUpdater;
            m_macdRepositoryUpdater = macdRepositoryUpdater;
            m_candlesService = candlesService;
            m_cancellationToken = cancellationToken;
            WorkerStatus = WorkerStatus.Created;
        }

        public async Task StartAsync(DateTime startTime)
        {
            m_currentTime = startTime;
            s_logger.LogInformation($"Start {nameof(StorageWorker)} for {m_currency} at {m_currentTime}");
            m_notificationService.Notify($"Start {nameof(StorageWorker)} for {m_currency} at {m_currentTime}");

            WorkerStatus = WorkerStatus.Running;
            try
            {
                await StartAsyncImpl();
                if (m_cancellationToken.IsCancellationRequested)
                {
                    s_logger.LogError($"{m_currency} {nameof(StorageWorker)} got cancellationRequest {m_currentTime}");
                    WorkerStatus = WorkerStatus.Cancelled;
                }
            }
            catch (OperationCanceledException)
            {
                s_logger.LogError($"{m_currency} {nameof(StorageWorker)} got cancellationRequest {m_currentTime}");
                WorkerStatus = WorkerStatus.Cancelled;
                m_notificationService.Notify($"{m_currency} {nameof(StorageWorker)} got cancellationRequest {m_currentTime}");
            }
            catch (Exception e)
            {
                WorkerStatus = WorkerStatus.Faulted;
                s_logger.LogError(e, $"{m_currency} {nameof(StorageWorker)} got exception {m_currentTime}");
                m_notificationService.Notify($"{m_currency} {nameof(StorageWorker)} got exception {m_currentTime}, {e.Message}");
            }

            await PersistRepositoriesDataIfNeeded();
            s_logger.LogInformation($"Stop {nameof(StorageWorker)} for {m_currency}");
        }

        private async Task StartAsyncImpl()
        {
            m_stopWatch.Restart();
            while (!m_cancellationToken.IsCancellationRequested)
            {
                await AddDataToRepositories();
                m_stopWatch.Stop();

                int elapsedSeconds = m_stopWatch.ElapsedSeconds;
                int timeToWaitInSeconds = m_iterationTimeInSeconds - elapsedSeconds;
                if (timeToWaitInSeconds < 0)
                {
                    throw new TimeoutException($"Add data to repositories took {elapsedSeconds}, timeout is {m_iterationTimeInSeconds}");
                }

                m_currentTime = await m_systemClock.Wait(m_cancellationToken, m_currency, timeToWaitInSeconds,
                    nameof(StorageWorker), m_currentTime);
                m_stopWatch.Restart();
            }
        }

        private async Task PersistRepositoriesDataIfNeeded()
        {
            if (m_persistData)
            {
                await PersistRepositoriesData();
            }
        }

        private async Task PersistRepositoriesData()
        {
            await m_candleRepositoryUpdater.PersistDataToFileAsync();
            await m_rsiRepositoryUpdater.PersistDataToFileAsync();
            await m_macdRepositoryUpdater.PersistDataToFileAsync();
        }

        private (DateTime previousTime, DateTime newTime) GetNewAndPreviousCandleTimes(CandleStorageObject candleDescription)
        {
            DateTime newTime = candleDescription.Candle.CloseTime;
            DateTime previousTime = newTime.Subtract(TimeSpan.FromMinutes(m_candleSize));

            return (previousTime, newTime);
        }

        private async Task AddDataToRepositories()
        {
            int amountOfOneMinuteKlines = m_candleSize + 1; // +1 in order to ignore last candle that didn't finish yet
            Memory<MyCandle> oneMinuteCandlesDescription = await m_candlesService.GetOneMinuteCandles(m_currency, amountOfOneMinuteKlines, m_currentTime);
            CandleStorageObject candle = BinanceKlineToMyCandleConverter.ConvertByCandleSize(oneMinuteCandlesDescription.Span, m_candleSize);
            (DateTime previousCandleTime, DateTime newCandleTime)  = GetNewAndPreviousCandleTimes(candle);
            m_candleRepositoryUpdater.AddInfo(candle, previousCandleTime, newCandleTime);
            m_rsiRepositoryUpdater.AddInfo(candle, previousCandleTime, newCandleTime);
            //m_macdRepositoryUpdater.AddInfo(candle, previousCandleTime, newCandleTime);
        }
    }
}