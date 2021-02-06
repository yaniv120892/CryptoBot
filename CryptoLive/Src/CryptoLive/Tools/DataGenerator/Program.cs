using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common;
using CsvHelper;
using Infra;
using Microsoft.Extensions.Logging;
using Services;
using Services.Abstractions;

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
                    MyCandle[] oldCandles = ReadOldCandles(fileName);
                    s_logger.LogInformation($"Merge old and new data");
                    var mergedCandles = (oldCandles.Union(newCandles)).Distinct().ToArray();
                    DeleteOldFile(fileName);
                    newCandles = mergedCandles;
                }

                s_logger.LogInformation($"Start write new data to {fileName}");
                await using var writer = new StreamWriter(fileName);
                {
                    await using var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);
                    {
                        csvWriter.Configuration.HasHeaderRecord = true;
                        await csvWriter.WriteRecordsAsync((IEnumerable) newCandles.ToArray());
                    }
                }
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

        private static void DeleteOldFile(string fileName)
        {
            s_logger.LogInformation($"Delete file {fileName}");
            File.Delete(fileName);
        }

        private static MyCandle[] ReadOldCandles(string fileName)
        {
            using var reader = new StreamReader(fileName);
            using var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture);
            csvReader.Configuration.HeaderValidated = null;
            var oldCandles = csvReader.GetRecords<MyCandle>();
            return oldCandles.ToArray();
        }

        private static string GetFileName(string currency, string candlesDataFolder)
        {
            return $"{candlesDataFolder}\\{currency}.csv";
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