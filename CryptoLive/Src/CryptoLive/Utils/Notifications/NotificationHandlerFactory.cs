using Common;
using Common.Notifications;
using Infra;
using Utils.Abstractions;

namespace Utils.Notifications
{
    public class NotificationHandlerFactory : INotificationHandlerFactory
    {
        private readonly INotificationServiceFactory m_notificationServiceFactory;

        public NotificationHandlerFactory(INotificationServiceFactory notificationServiceFactory)
        {
            m_notificationServiceFactory = notificationServiceFactory;
        }

        public INotificationHandler Create(NotificationType notificationType, NotificationHandlerType notificationHandlerType)
        {
            INotificationService notificationService = m_notificationServiceFactory.Create(notificationType);
            return new NotificationHandler(notificationService, $"Notification Message {notificationHandlerType}");
        }
    }
}