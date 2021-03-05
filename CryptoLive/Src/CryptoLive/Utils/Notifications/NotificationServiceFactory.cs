using System;
using Common.Notifications;
using Infra;
using Infra.NotificationServices;
using Utils.Abstractions;

namespace Utils.Notifications
{
    public class NotificationServiceFactory : INotificationServiceFactory
    {
        private readonly string m_twilioWhatsAppSender;
        private readonly string m_whatsAppRecipient;
        private readonly string m_twilioSsid;
        private readonly string m_twilioAuthToken;
        private readonly string m_telegramChatId;
        private readonly string m_telegramAuthToken;

        public NotificationServiceFactory(
            string twilioWhatsAppSender, 
            string whatsAppRecipient, 
            string twilioSsid, 
            string twilioAuthToken,
            string telegramChatId, 
            string telegramAuthToken)
        {
            m_twilioWhatsAppSender = twilioWhatsAppSender;
            m_whatsAppRecipient = whatsAppRecipient;
            m_twilioSsid = twilioSsid;
            m_twilioAuthToken = twilioAuthToken;
            m_telegramChatId = telegramChatId;
            m_telegramAuthToken = telegramAuthToken;
        }

        public INotificationService Create(NotificationType notificationType)
        {
            return notificationType switch
            {
                NotificationType.Disable => new EmptyNotificationService(),
                NotificationType.WhatsApp => new WhatsAppNotificationService(m_twilioWhatsAppSender,
                    m_whatsAppRecipient, m_twilioSsid, m_twilioAuthToken),
                NotificationType.Telegram => new TelegramNotificationService(m_telegramChatId, m_telegramAuthToken),
                _ => throw new ArgumentException($"Unknown NotificationType - {notificationType}")
            };
        }
    }
}