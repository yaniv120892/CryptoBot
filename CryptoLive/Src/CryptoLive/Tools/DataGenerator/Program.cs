using System;
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

        public static async Task Main(string[] args)
        {
            DataGeneratorParameters dataGeneratorParameters = AppParametersLoader<DataGeneratorParameters>.Load(s_configFile);
            ICandlesService candleService = CreateCandleService(dataGeneratorParameters);
            const int candlesAmount = 999;
            CreateDirectoryIfNotExist(dataGeneratorParameters.CandlesDataFolder);
            foreach (string currency in dataGeneratorParameters.Currencies)
            {
                string fileName = GetFileName(currency, dataGeneratorParameters.CandlesDataFolder);
                MyCandle[] newCandles = (await candleService.GetOneMinuteCandles(currency, candlesAmount, DateTime.UtcNow)).ToArray();
                if (File.Exists(fileName))
                {
                    MyCandle[] oldCandles = CsvFileAccess.ReadCsv<MyCandle>(fileName);
                    s_logger.LogInformation($"Merge old and new data");
                    var mergedCandles = (oldCandles.Union(newCandles)).Distinct().ToArray();
                    CsvFileAccess.DeleteFile(fileName);
                    newCandles = mergedCandles;
                }

                s_logger.LogInformation($"Start write new data to {fileName}");
                await CsvFileAccess.WriteCsvAsync(fileName, newCandles);
                s_logger.LogInformation($"Done create {fileName}");
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