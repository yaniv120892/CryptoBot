using System;
using Infra;
using Utils.Abstractions;

namespace Utils.Notifications
{
    public class NotificationHandler : INotificationHandler
    {
        private readonly INotificationService m_notificationService;
        private readonly string m_notificationMessage;

        public NotificationHandler(INotificationService notificationService, string notificationMessage)
        {
            m_notificationService = notificationService;
            m_notificationMessage = notificationMessage;
        }

        public bool NotifyIfNeeded(Func<bool> condition, string currency)
        {
            if (condition.Invoke())
            {
                string body = CreateMessageBody(currency);
                m_notificationService.Notify(body);
                return true;
            }

            return false;
        }

        private string CreateMessageBody(string currency) =>
            $"{currency}: {m_notificationMessage}";
    }
}