using Common;
using Common.Notifications;
using Infra;

namespace Utils.Abstractions
{
    public interface INotificationServiceFactory
    {
        INotificationService Create(NotificationType notificationType);
    }
}