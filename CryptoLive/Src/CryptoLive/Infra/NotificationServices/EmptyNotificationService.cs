using Microsoft.Extensions.Logging;

namespace Infra.NotificationServices
{
    public class EmptyNotificationService : INotificationService
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<EmptyNotificationService>();
        
        public void Notify(string body)
        {
            s_logger.LogInformation($"Notification is disabled, don't send");
            s_logger.LogInformation(body);
        }
    }
}