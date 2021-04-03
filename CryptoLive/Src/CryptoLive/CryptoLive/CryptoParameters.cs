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
        public int BotDelayTimeInMinutes { get; set; }
        public string TelegramChatId { get; }
        public string TelegramAuthToken { get; }
        public string CryptoBotName { get; }

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
            BotDelayTimeInMinutes = int.Parse(applicationSection[nameof(BotDelayTimeInMinutes)]);
            TelegramChatId = applicationSection[nameof(TelegramChatId)];
            TelegramAuthToken = applicationSection[nameof(TelegramAuthToken)];
            CryptoBotName = applicationSection[nameof(CryptoBotName)];
        }

        public override string ToString()
        {
            return $"{base.ToString()},\n" +
                   $"Currencies {string.Join(", ", Currencies)},\n" +
                   $"Notification Type: {NotificationType.ToString()},\n" +
                   $"Rsi Size: {RsiSize},\n" +
                   $"Bot Delay Time in minutes: {BotDelayTimeInMinutes}\n" +
                   $"Crypto Bot Name: {CryptoBotName}";
        }
    }
}