using System;
using System.Globalization;
using Common.Abstractions;
using Microsoft.Extensions.Configuration;
using Utils;

namespace DemoCryptoLive
{
    public class DemoCryptoParameters : CryptoParametersBase
    {
        public string BinanceApiKey { get; }
        public string BinanceApiSecretKey { get; }
        public string[] Currencies { get; }
        public string CandlesDataFolder { get; }
        public int RsiSize { get; }
        public int MeanAverageSize { get; set; }
        public string CalculatedDataFolder { get; }
        public DateTime BotStartTime { get; }
        public DateTime BotEndTime { get; }


        public DemoCryptoParameters(IConfigurationSection applicationSection):base(applicationSection)
        {
            BinanceApiKey = applicationSection[nameof(BinanceApiKey)];
            BinanceApiSecretKey = applicationSection[nameof(BinanceApiSecretKey)];
            Currencies = applicationSection[nameof(Currencies)].Split(",");
            CandlesDataFolder = applicationSection[nameof(CandlesDataFolder)];
            CalculatedDataFolder = applicationSection[nameof(CalculatedDataFolder)];
            RsiSize = int.Parse(applicationSection[nameof(RsiSize)]);
            MeanAverageSize = int.Parse(applicationSection[nameof(MeanAverageSize)]);
            BotStartTime = DateTime.ParseExact(applicationSection[nameof(BotStartTime)], 
                CsvFileAccess.DateTimeFormat, CultureInfo.InvariantCulture);
            BotEndTime = DateTime.ParseExact(applicationSection[nameof(BotEndTime)], 
                CsvFileAccess.DateTimeFormat, CultureInfo.InvariantCulture);
        }

        public override string ToString()
        {
            return $"{base.ToString()},\n" +
                   $"Bot start time: {BotStartTime:dd/MM/yyyy HH:mm:ss},\n" +
                   $"Bot end time  : {BotEndTime:dd/MM/yyyy HH:mm:ss},\n" +
                   $"Rsi size      : {RsiSize},\n" +
                   $"Mean average size      : {MeanAverageSize},\n" +
                   $"Currencies    : {string.Join(", ", Currencies)}";
        }
    }
}