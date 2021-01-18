using System;
using Infra;
using Microsoft.Extensions.Logging;
using Utils.Abstractions;

namespace Utils.NotificationHandlers
{
    public class ChangePriceNotificationHandler : INotificationHandler
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<ChangePriceNotificationHandler>();

        private readonly INotificationService m_notificationService;
        private readonly decimal m_minPriceRangeToNotify;
        private readonly decimal m_maxPriceRangeToNotify;
        
        public ChangePriceNotificationHandler(INotificationService notificationService,
            decimal originPrice, 
            decimal priceChangeToNotify)
        {
            if (priceChangeToNotify > 100 || priceChangeToNotify < 1)
            {
                throw new ArgumentException("Change for price notification should be value between 1-100");
            }

            m_notificationService = notificationService;
            m_minPriceRangeToNotify = originPrice * (100 - priceChangeToNotify) / 100;
            m_maxPriceRangeToNotify = originPrice * (100 + priceChangeToNotify) / 100;
            s_logger.LogInformation($"Will notify when price drops below {m_minPriceRangeToNotify} or increase above {m_maxPriceRangeToNotify}");
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

        private string CreateMessageBody(decimal indicator, string symbol)
        {
            if (IsPriceBelowMin(indicator))
            {
                return $"{symbol}: Price is below {m_minPriceRangeToNotify}, value is {indicator}";
            }
            
            if (IsPriceAboveMax(indicator))
            {
                return $"{symbol}: Price is above {m_maxPriceRangeToNotify}, value is {indicator}";
            }

            throw new Exception($"Price is between {m_minPriceRangeToNotify}-{m_maxPriceRangeToNotify},The value is {indicator}." +
                                $" Should not send notification");
        }

        private bool ShouldNotify(decimal newPrice) => IsPriceBelowMin(newPrice) || IsPriceAboveMax(newPrice);
        
        private bool IsPriceAboveMax(decimal indicator) => indicator >= m_maxPriceRangeToNotify;

        private bool IsPriceBelowMin(decimal indicator) => indicator <= m_minPriceRangeToNotify;
    }
}