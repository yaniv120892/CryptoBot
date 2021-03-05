using System;
using Common.Abstractions;
using Common.Notifications;
using Microsoft.Extensions.Configuration;

namespace CryptoLive
{
    public class CryptoLiveParameters : CryptoParametersBase
    {
        public string TwilioWhatsAppSender { get; }
        public string WhatsAppRecipient { get; }
        public string TwilioSsid { get; }
        public string TwilioAuthToken { get; }
        public string BinanceApiKey { get; }
        public string BinanceApiSecretKey { get; }
        public string[] Currencies { get; }
        public NotificationType NotificationType { get; set; }
        public int RsiSize { get; set; }
        public int FastEmaSize { get; set; }
        public int SlowEmaSize { get; set; }
        public int SignalSize { get; set; }
        public int MaxMacdPollingTime { get; set; }
        public int BotDelayTime { get; set; }
        public string TelegramChatId { get; }
        public string TelegramAuthToken { get; }

        public CryptoLiveParameters(IConfigurationSection applicationSection) : base(applicationSection)
        {
            TwilioWhatsAppSender = applicationSection[nameof(TwilioWhatsAppSender)];
            WhatsAppRecipient = applicationSection[nameof(WhatsAppRecipient)];
            TwilioSsid = applicationSection[nameof(TwilioSsid)];
            TwilioAuthToken = applicationSection[nameof(TwilioAuthToken)];
            BinanceApiKey = applicationSection[nameof(BinanceApiKey)];
            BinanceApiSecretKey = applicationSection[nameof(BinanceApiSecretKey)];
            Currencies = applicationSection[nameof(Currencies)].Split(",");
            NotificationType = Enum.Parse<NotificationType>(applicationSection[nameof(NotificationType)]);
            RsiSize = int.Parse(applicationSection[nameof(RsiSize)]);
            FastEmaSize = int.Parse(applicationSection[nameof(FastEmaSize)]);
            SlowEmaSize = int.Parse(applicationSection[nameof(SlowEmaSize)]);
            SignalSize = int.Parse(applicationSection[nameof(SignalSize)]);
            MaxMacdPollingTime = int.Parse(applicationSection[nameof(MaxMacdPollingTime)]);
            BotDelayTime = int.Parse(applicationSection[nameof(BotDelayTime)]);
            TelegramChatId = applicationSection[nameof(TelegramChatId)];
            TelegramAuthToken = applicationSection[nameof(TelegramAuthToken)];
        }
    }
}