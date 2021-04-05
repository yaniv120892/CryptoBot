using Microsoft.Extensions.Configuration;

namespace Common.Abstractions
{
    public abstract class CryptoParametersBase
    {
        protected CryptoParametersBase(IConfigurationSection applicationSection)
        {
            PriceChangeToNotify = int.Parse(applicationSection[nameof(PriceChangeToNotify)]);
            MaxRsiToNotify = int.Parse(applicationSection[nameof(MaxRsiToNotify)]);
            CandleSize = int.Parse(applicationSection[nameof(CandleSize)]);
            RsiMemorySize = int.Parse(applicationSection[nameof(RsiMemorySize)]);
        }

        public int CandleSize { get; set; }
        public decimal MaxRsiToNotify { get; set; }
        public int RsiMemorySize { get; set; }
        public decimal PriceChangeToNotify { get; set; }
        
        public override string ToString()
        {
            return $"Price signal      : {PriceChangeToNotify}%,\n" +
                   $"Rsi signal        : {MaxRsiToNotify},\n" +
                   $"Candle size       : {CandleSize},\n" +
                   $"Rsi history       : {RsiMemorySize/60} hours";
        }
    }
}