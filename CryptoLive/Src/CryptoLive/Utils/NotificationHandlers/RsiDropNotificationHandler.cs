using Infra;
using Microsoft.Extensions.Logging;
using Utils.Abstractions;

namespace Utils.NotificationHandlers
{
    public class RsiDropNotificationHandler : INotificationHandler
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<RsiDropNotificationHandler>();
        
        private readonly decimal m_maxRsi;
        private readonly INotificationService m_notificationService;

        public RsiDropNotificationHandler(INotificationService notificationService,decimal maxRsi)
        {
            m_notificationService = notificationService;
            m_maxRsi = maxRsi;
            s_logger.LogInformation($"Will notify when rsi drops below {m_maxRsi}");
        }

        public bool NotifyIfNeeded(decimal indicator, string symbol)
        {
            if (ShouldNotify(indicator))
            {
                string body = CreateMessageBody(indicator, symbol);
                m_notificationService.Notify(body);
                return true;
            }

            return false;
        }

        private string CreateMessageBody(decimal indicator, string symbol) =>
            $"{symbol}: RSI is below {m_maxRsi}, value is {indicator}";

        private bool ShouldNotify(decimal indicator) => indicator < m_maxRsi;
    }
}