using System;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Utils;

namespace DataGenerator
{
    public class DataGeneratorParameters
    {
        public string BinanceApiKey { get; }
        public string BinanceApiSecretKey { get; }
        public string CandlesDataFolder { get; }
        public string[] Currencies { get; }
        public DateTime CandlesStartTime { get; }

        public DataGeneratorParameters(IConfigurationSection applicationSection)
        {
            BinanceApiKey = applicationSection[nameof(BinanceApiKey)];
            BinanceApiSecretKey = applicationSection[nameof(BinanceApiSecretKey)];
            CandlesDataFolder = applicationSection[nameof(CandlesDataFolder)];
            Currencies = applicationSection[nameof(Currencies)].Split(",");
            CandlesStartTime = DateTime.ParseExact(applicationSection[nameof(CandlesStartTime)], 
                CsvFileAccess.DateTimeFormat, CultureInfo.InvariantCulture);
        }
    }
}