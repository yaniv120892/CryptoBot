using Infra;
using Microsoft.Extensions.Logging;
using Utils.Abstractions;

namespace Utils.NotificationHandlers
{
    public class RedCandleNotificationHandler : INotificationHandler
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<RedCandleNotificationHandler>();

        private readonly INotificationService m_notificationService;

        public RedCandleNotificationHandler(INotificationService notificationService)
        {
            m_notificationService = notificationService;
            s_logger.LogInformation("Will notify if candle is red");
        }

        public bool NotifyIfNeeded(decimal indicator, string symbol)
        {
            if (ShouldNotify(indicator))
            {
                string body = CreateMessageBody(symbol);
                m_notificationService.Notify(body);
                return true;
            }

            return false;
        }

        private static string CreateMessageBody(string symbol) => $"{symbol}: Got red candle";

        private static bool ShouldNotify(decimal indicator) => indicator == 1;
    }
}