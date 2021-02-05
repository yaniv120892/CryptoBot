using Microsoft.Extensions.Configuration;

namespace DemoCryptoLive
{
    public class DemoCryptoParameters
    {
        public int PriceChangeToNotify { get; }
        public string BinanceApiKey { get; }
        public string BinanceApiSecretKey { get; }
        public int DelayTimeIterationsInSeconds { get; set; }
        public int CandleSize { get; set; }
        public string[] Currencies { get; }
        public decimal MaxRsiToNotify { get; }
        public string CandlesDataFolder { get; set; }
        public int RsiMemorySize { get; set; }
        public int RsiSize { get; set; }
        public int FastEmaSize { get; set; }
        public int SlowEmaSize { get; set; }
        public int SignalSize { get; set; }
        public int MaxMacdPollingTime { get; set; }


        public DemoCryptoParameters(IConfigurationSection applicationSection)
        {
            CandleSize = int.Parse(applicationSection[nameof(CandleSize)]);
            PriceChangeToNotify = int.Parse(applicationSection[nameof(PriceChangeToNotify)]);
            MaxRsiToNotify = int.Parse(applicationSection[nameof(MaxRsiToNotify)]);
            BinanceApiKey = applicationSection[nameof(BinanceApiKey)];
            BinanceApiSecretKey = applicationSection[nameof(BinanceApiSecretKey)];
            DelayTimeIterationsInSeconds = int.Parse(applicationSection[nameof(DelayTimeIterationsInSeconds)]);
            Currencies = applicationSection[nameof(Currencies)].Split(",");
            CandlesDataFolder = applicationSection[nameof(CandlesDataFolder)];
            RsiMemorySize = int.Parse(applicationSection[nameof(RsiMemorySize)]);
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