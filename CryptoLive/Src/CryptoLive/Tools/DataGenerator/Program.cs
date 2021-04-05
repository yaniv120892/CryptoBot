using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Infra;
using Microsoft.Extensions.Logging;
using Services;
using Services.Abstractions;
using Utils;

namespace DataGenerator
{
    public class Program
    {
        private static readonly string s_configFile = "appsettings.json";
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<Program>();
        private static readonly int s_requestsIntervalInMilliseconds = 1 * 1000;
        
        public static async Task Main(string[] args)
        {
            DataGeneratorParameters dataGeneratorParameters = AppParametersLoader<DataGeneratorParameters>.Load(s_configFile);
            ICandlesService candleService = CreateCandleService(dataGeneratorParameters);
            CreateDirectoryIfNotExist(dataGeneratorParameters.CandlesDataFolder);
            List<Task> tasks = new List<Task>();
            foreach (string currency in dataGeneratorParameters.Currencies)
            {
                Task task = GetAndPersistNewData(currency, dataGeneratorParameters, candleService);
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
            s_logger.LogInformation("Done");
        }

        private static async Task GetAndPersistNewData(string currency, DataGeneratorParameters dataGeneratorParameters,
            ICandlesService candleService)
        {
            string fileName = GetFileName(currency, dataGeneratorParameters.CandlesDataFolder);
            DateTime currentTime = dataGeneratorParameters.CandlesStartTime;
            DateTime endTime = dataGeneratorParameters.CandlesEndTime;
            while(currentTime < endTime)
            {
                s_logger.LogInformation($"{currency}: Start Download data for {currentTime:dd/MM/yyyy HH:mm:ss}");
                MyCandle[] newCandles = (await candleService.GetOneMinuteCandles(currency, currentTime)).ToArray();
            
                if (File.Exists(fileName))
                {
                    MyCandle[] oldCandles = CsvFileAccess.ReadCsv<MyCandle>(fileName);
                    var mergedCandles = oldCandles.Union(newCandles).Distinct().ToArray();
                    CsvFileAccess.DeleteFile(fileName);
                    newCandles = mergedCandles;
                }

                await CsvFileAccess.WriteCsvAsync(fileName, newCandles.Distinct().ToArray());
                s_logger.LogInformation($"{currency}: Done Download data for {currentTime:dd/MM/yyyy HH:mm:ss}");
                currentTime = currentTime.AddMinutes(999);
                await Task.Delay(s_requestsIntervalInMilliseconds);
            }
        }

        private static ICandlesService CreateCandleService(DataGeneratorParameters dataGeneratorParameters)
        {
            ICurrencyClientFactory currencyClientFactory =
                new CurrencyClientFactory(dataGeneratorParameters.BinanceApiKey,
                    dataGeneratorParameters.BinanceApiSecretKey);
            return new BinanceCandleService(currencyClientFactory);
        }

        private static string GetFileName(string currency, string candlesDataFolder)
        {
            return Path.Combine(candlesDataFolder, $"{currency}.csv");
        }

        private static void CreateDirectoryIfNotExist(string folderName)
        {
            if (!Directory.Exists(folderName))
            {
                s_logger.LogInformation($"Create folder {folderName}");
                Directory.CreateDirectory(folderName);
            }
        }
    }
}