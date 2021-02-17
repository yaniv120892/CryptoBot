using Common.Abstractions;
using Microsoft.Extensions.Configuration;

namespace DemoCryptoLive
{
    public class DemoCryptoParameters : CryptoParametersBase
    {
        public string BinanceApiKey { get; }
        public string BinanceApiSecretKey { get; }
        public string[] Currencies { get; }
        public string CandlesDataFolder { get; set; }
        public int RsiSize { get; set; }
        public int FastEmaSize { get; set; }
        public int SlowEmaSize { get; set; }
        public int SignalSize { get; set; }
        public int MaxMacdPollingTime { get; set; }
        public string CalculatedDataFolder { get; set; }


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
        }

        public override string ToString()
        {
            return $"Candle size in minutes: {CandleSize}, " +
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