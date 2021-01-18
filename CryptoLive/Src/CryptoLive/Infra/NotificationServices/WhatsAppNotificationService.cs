using System;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Infra.NotificationServices
{
    public class WhatsAppNotificationService : INotificationService 
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<WhatsAppNotificationService>();

        private readonly string m_whatsAppSender;
        private readonly string m_whatsAppReceiver;

        public WhatsAppNotificationService(string whatsAppSender, 
            string whatsAppReceiver,
            string ssid, 
            string authToken)
        {
            m_whatsAppSender = whatsAppSender;
            m_whatsAppReceiver = whatsAppReceiver;
            TwilioClient.Init(ssid, authToken);
        }
        
        public void Notify(string body)
        {
            try
            {
                s_logger.LogDebug($"Start send whatsApp message to {m_whatsAppReceiver}, body: {body}");
                NotifyImpl(body);
                s_logger.LogDebug("Done send WhatsApp message");
            }
            catch (Exception e)
            {
                s_logger.LogError($"Failed send WhatsApp message, {e.Message}");
                throw new Exception("Failed send WhatsApp message");
            }
        }

        private void NotifyImpl(string body)
        {
            var message = MessageResource.Create(
                from: new PhoneNumber($"whatsapp:{m_whatsAppSender}"),
                to: new PhoneNumber($"whatsapp:{m_whatsAppReceiver}"),
                body: body
            );
            
            s_logger.LogTrace($"Message status:{message.Status}");
        }
    }
}