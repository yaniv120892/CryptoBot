using System;
using Microsoft.Extensions.Configuration;

namespace Common
{
    public class CryptoLiveParameters
    {
        public decimal BasePrice { get; set; }
        public int PriceChangeToNotify { get; }
        public string TwilioWhatsAppSender { get; }
        public string WhatsAppRecipient { get; }
        public string TwilioSsid { get; }
        public string TwilioAuthToken { get; }
        public string BinanceApiKey { get; }
        public string BinanceApiSecretKey { get; }
        public int DelayTimeIterationsInSeconds { get; set; }
        public int CandleSize { get; set; }
        public string[] Currencies { get; }
        public AppMode AppMode { get; set; }
        public decimal MaxRsiToNotify { get; }
        public NotificationType NotificationType { get; set; }
        public int RsiCandlesAmount { get; set; }

        public CryptoLiveParameters(IConfigurationSection applicationSection)
        {
            BasePrice = decimal.Parse(applicationSection[nameof(BasePrice)]);
            PriceChangeToNotify = int.Parse(applicationSection[nameof(PriceChangeToNotify)]);
            MaxRsiToNotify = int.Parse(applicationSection[nameof(MaxRsiToNotify)]);
            RsiCandlesAmount = int.Parse(applicationSection[nameof(RsiCandlesAmount)]);
            TwilioWhatsAppSender = applicationSection[nameof(TwilioWhatsAppSender)];
            WhatsAppRecipient = applicationSection[nameof(WhatsAppRecipient)];
            TwilioSsid = applicationSection[nameof(TwilioSsid)];
            TwilioAuthToken = applicationSection[nameof(TwilioAuthToken)];
            BinanceApiKey = applicationSection[nameof(BinanceApiKey)];
            BinanceApiSecretKey = applicationSection[nameof(BinanceApiSecretKey)];
            DelayTimeIterationsInSeconds = int.Parse(applicationSection[nameof(DelayTimeIterationsInSeconds)]);
            CandleSize = int.Parse(applicationSection[nameof(CandleSize)]);
            Currencies = applicationSection[nameof(Currencies)].Split(",");
            AppMode = Enum.Parse<AppMode>(applicationSection[nameof(AppMode)]);
            NotificationType = Enum.Parse<NotificationType>(applicationSection[nameof(NotificationType)]);
        }
    }
}