using System;
using Common.Notifications;
using Microsoft.Extensions.Configuration;

namespace CryptoLive
{
    public class CryptoLiveParameters
    {
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
        public decimal MaxRsiToNotify { get; }
        public NotificationType NotificationType { get; set; }
        public int RsiMemorySize { get; set; }
        public int RsiSize { get; set; }
        public int FastEmaSize { get; set; }
        public int SlowEmaSize { get; set; }
        public int SignalSize { get; set; }
        public int MaxMacdPollingTime { get; set; }

        public CryptoLiveParameters(IConfigurationSection applicationSection)
        {
            PriceChangeToNotify = int.Parse(applicationSection[nameof(PriceChangeToNotify)]);
            MaxRsiToNotify = int.Parse(applicationSection[nameof(MaxRsiToNotify)]);
            TwilioWhatsAppSender = applicationSection[nameof(TwilioWhatsAppSender)];
            WhatsAppRecipient = applicationSection[nameof(WhatsAppRecipient)];
            TwilioSsid = applicationSection[nameof(TwilioSsid)];
            TwilioAuthToken = applicationSection[nameof(TwilioAuthToken)];
            BinanceApiKey = applicationSection[nameof(BinanceApiKey)];
            BinanceApiSecretKey = applicationSection[nameof(BinanceApiSecretKey)];
            DelayTimeIterationsInSeconds = int.Parse(applicationSection[nameof(DelayTimeIterationsInSeconds)]);
            CandleSize = int.Parse(applicationSection[nameof(CandleSize)]);
            Currencies = applicationSection[nameof(Currencies)].Split(",");
            NotificationType = Enum.Parse<NotificationType>(applicationSection[nameof(NotificationType)]);
            RsiMemorySize = int.Parse(applicationSection[nameof(RsiMemorySize)]);
            RsiSize = int.Parse(applicationSection[nameof(RsiSize)]);
            FastEmaSize = int.Parse(applicationSection[nameof(FastEmaSize)]);
            SlowEmaSize = int.Parse(applicationSection[nameof(SlowEmaSize)]);
            SignalSize = int.Parse(applicationSection[nameof(SignalSize)]);
            MaxMacdPollingTime = int.Parse(applicationSection[nameof(MaxMacdPollingTime)]);
        }
    }
}