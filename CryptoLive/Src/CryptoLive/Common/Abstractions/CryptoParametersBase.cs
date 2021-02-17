using Microsoft.Extensions.Configuration;

namespace Common.Abstractions
{
    public abstract class CryptoParametersBase
    {
        protected CryptoParametersBase(IConfigurationSection applicationSection)
        {
            PriceChangeToNotify = int.Parse(applicationSection[nameof(PriceChangeToNotify)]);
            MaxRsiToNotify = int.Parse(applicationSection[nameof(MaxRsiToNotify)]);
            DelayTimeIterationsInSeconds = int.Parse(applicationSection[nameof(DelayTimeIterationsInSeconds)]);
            CandleSize = int.Parse(applicationSection[nameof(CandleSize)]);
            RsiMemorySize = int.Parse(applicationSection[nameof(RsiMemorySize)]);
            MinutesToWaitBeforePollingPrice = int.Parse(applicationSection[nameof(MinutesToWaitBeforePollingPrice)]);        
        }

        public int CandleSize { get; set; }
        public decimal MaxRsiToNotify { get; set; }
        public int RsiMemorySize { get; set; }
        public int DelayTimeIterationsInSeconds { get; set; }
        public decimal PriceChangeToNotify { get; set; }
        public int MinutesToWaitBeforePollingPrice { get; set; }
    }
}