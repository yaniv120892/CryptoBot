using Microsoft.Extensions.Configuration;

namespace DemoCryptoLive
{
    public class CryptoDemoParameters
    {
        public int PriceChangeToNotify { get; }
        public string BinanceApiKey { get; }
        public string BinanceApiSecretKey { get; }
        public int DelayTimeIterationsInSeconds { get; set; }
        public int CandleSize { get; set; }
        public string[] Currencies { get; }
        public decimal MaxRsiToNotify { get; }
        public string CandlesDataFolder { get; set; }
        public int RsiCandlesAmount { get; set; }

        public CryptoDemoParameters(IConfigurationSection applicationSection)
        {
            CandleSize = int.Parse(applicationSection[nameof(CandleSize)]);
            PriceChangeToNotify = int.Parse(applicationSection[nameof(PriceChangeToNotify)]);
            MaxRsiToNotify = int.Parse(applicationSection[nameof(MaxRsiToNotify)]);
            RsiCandlesAmount = int.Parse(applicationSection[nameof(RsiCandlesAmount)]);
            BinanceApiKey = applicationSection[nameof(BinanceApiKey)];
            BinanceApiSecretKey = applicationSection[nameof(BinanceApiSecretKey)];
            DelayTimeIterationsInSeconds = int.Parse(applicationSection[nameof(DelayTimeIterationsInSeconds)]);
            Currencies = applicationSection[nameof(Currencies)].Split(",");
            CandlesDataFolder = applicationSection[nameof(CandlesDataFolder)];
        }

        public override string ToString()
        {
            return $"Candle size in minutes: {CandleSize}, " +
                   $"Price change to notify: {PriceChangeToNotify}, " +
                   $"Max rsi to notify: {MaxRsiToNotify}, " +
                   $"Rsi candles amount: {RsiCandlesAmount}, " +
                   $"Currencies: {string.Join(", ", Currencies)}";
        }
    }
}