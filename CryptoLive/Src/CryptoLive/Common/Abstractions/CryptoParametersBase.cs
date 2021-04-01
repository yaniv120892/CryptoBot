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
            MinutesToWaitBeforePollingPrice = int.Parse(applicationSection[nameof(MinutesToWaitBeforePollingPrice)]);        
        }

        public int CandleSize { get; set; }
        public decimal MaxRsiToNotify { get; set; }
        public int RsiMemorySize { get; set; }
        public decimal PriceChangeToNotify { get; set; }
        public int MinutesToWaitBeforePollingPrice { get; set; }
        
        public override string ToString()
        {
            return $"Price Change To Notify: {PriceChangeToNotify}%,\n" +
                   $"Max Rsi To Notify: {MaxRsiToNotify},\n" +
                   $"Candle Size: {CandleSize},\n" +
                   $"Rsi Memory Size: {RsiMemorySize},\n" +
                   $"Minutes To Wait Before Polling Price: {MinutesToWaitBeforePollingPrice}";
        }
    }
}