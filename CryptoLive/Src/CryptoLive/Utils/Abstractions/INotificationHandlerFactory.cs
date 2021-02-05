using Common;
using Common.Notifications;
using Utils.Notifications;

namespace Utils.Abstractions
{
    public interface INotificationHandlerFactory
    {
        INotificationHandler Create(NotificationType notificationType, NotificationHandlerType notificationHandlerType);
    }
}