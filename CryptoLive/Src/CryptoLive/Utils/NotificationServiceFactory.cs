using System;
using Common;
using Infra;
using Infra.NotificationServices;

namespace Utils
{
    public class NotificationServiceFactory
    {
        public static INotificationService CreateNotificationService(NotificationType notificationType, string twilioWhatsAppSender=null,
            string whatsAppRecipient=null, string twilioSsid=null, string twilioAuthToken=null)
        {
            return notificationType switch
            {
                NotificationType.Disable => new EmptyNotificationService(),
                NotificationType.WhatsApp => new WhatsAppNotificationService(twilioWhatsAppSender,
                    whatsAppRecipient, twilioSsid, twilioAuthToken),
                _ => throw new ArgumentException($"Unknown NotificationType - {notificationType}")
            };
        }
    }
}