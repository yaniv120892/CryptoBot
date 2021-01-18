using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Common;
using CsvHelper;
using Infra;
using Microsoft.Extensions.Logging;
using Utils.Abstractions;
using Utils.CurrencyService;

namespace DataGenerator
{
    public class Program
    {
        private static readonly string s_configFile = "appsettings.json";
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<Program>();

        public static async Task Main(string[] args)
        {
            DataGeneratorParameters dataGeneratorParameters = AppParametersLoader<DataGeneratorParameters>.Load(s_configFile);
            ICurrencyService currencyService = new BinanceCurrencyService(dataGeneratorParameters.BinanceApiKey, dataGeneratorParameters.BinanceApiSecretKey);
            const int candleSizeInMinutes = 1;
            const int candlesAmount = 999;
            CreateDirectoryIfNotExist(dataGeneratorParameters.CandlesDataFolder);
            foreach (string currency in  dataGeneratorParameters.Currencies)
            {
                string fileName = GetFileName(currency, dataGeneratorParameters.CandlesDataFolder);
                MyCandle[] candles = await currencyService.GetCandlesAsync(currency, candleSizeInMinutes, candlesAmount, DateTime.Now);
                if (File.Exists(fileName))
                {
                    s_logger.LogInformation($"Start append data to {fileName}");
                    await using var stream = File.Open(fileName, FileMode.Append);
                    await using var writer = new StreamWriter(stream);
                    {
                        await using var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);
                        {
                            csvWriter.Configuration.HasHeaderRecord = false;
                            await csvWriter.WriteRecordsAsync((IEnumerable) candles);
                        }
                    }
                }
                else
                {
                    await using var writer = new StreamWriter(fileName);
                    {
                        await using var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);
                        {
                            await csvWriter.WriteRecordsAsync((IEnumerable) candles);
                        }
                    }
                }
                
                s_logger.LogInformation($"Done create {fileName}");
            }
        }

        private static string GetFileName(string currency, string candlesDataFolder)
        {
            return $"{candlesDataFolder}/{currency}.csv";
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