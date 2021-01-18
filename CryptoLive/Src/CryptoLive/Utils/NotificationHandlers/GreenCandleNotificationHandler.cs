using Infra;
using Microsoft.Extensions.Logging;
using Utils.Abstractions;

namespace Utils.NotificationHandlers
{
    public class GreenCandleNotificationHandler : INotificationHandler
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<GreenCandleNotificationHandler>();
        
        private readonly INotificationService m_notificationService;

        public GreenCandleNotificationHandler(INotificationService notificationService)
        {
            m_notificationService = notificationService;
            s_logger.LogInformation("Will notify if candle is green and previous high candle is lower than current close candle");
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

        private string CreateMessageBody(string symbol) => 
            $"{symbol}: Got green candle and current candle close is higher than previous candle high";

        private bool ShouldNotify(decimal indicator) => indicator == 1;
    }
}