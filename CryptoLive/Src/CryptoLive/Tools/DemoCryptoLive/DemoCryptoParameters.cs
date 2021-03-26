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
        public int FastEmaSize { get;  }
        public int SlowEmaSize { get;  }
        public int SignalSize { get; }
        public int MaxMacdPollingTime { get; }
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
            FastEmaSize = int.Parse(applicationSection[nameof(FastEmaSize)]);
            SlowEmaSize = int.Parse(applicationSection[nameof(SlowEmaSize)]);
            SignalSize = int.Parse(applicationSection[nameof(SignalSize)]);
            MaxMacdPollingTime = int.Parse(applicationSection[nameof(MaxMacdPollingTime)]);
            BotStartTime = DateTime.ParseExact(applicationSection[nameof(BotStartTime)], 
                CsvFileAccess.DateTimeFormat, CultureInfo.InvariantCulture);
            BotEndTime = DateTime.ParseExact(applicationSection[nameof(BotEndTime)], 
                CsvFileAccess.DateTimeFormat, CultureInfo.InvariantCulture);
        }

        public override string ToString()
        {
            return $"Bot start time: {BotStartTime}, " +
                   $"Bot end time: {BotEndTime}, " +
                   $"Candle size in minutes: {CandleSize}, " +
                   $"Price change to notify: {PriceChangeToNotify}, " +
                   $"Max rsi to notify: {MaxRsiToNotify}, " +
                   $"Rsi size : {RsiSize}, " +
                   $"Fast EMA size : {FastEmaSize}, " +
                   $"Slow EMA size : {SlowEmaSize}, " +
                   $"Signal size : {SignalSize}, " +
                   $"Max macd polling time : {MaxMacdPollingTime}, " +
                   $"Rsi memory size: {RsiMemorySize}, " +
                   $"Currencies: {string.Join(", ", Currencies)}";
        }
    }
}