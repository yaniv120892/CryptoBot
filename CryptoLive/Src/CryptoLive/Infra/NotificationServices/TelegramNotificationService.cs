using System;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace Infra.NotificationServices
{
    public class TelegramNotificationService :INotificationService
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<TelegramNotificationService>();

        private readonly string m_telegramChatId;
        private readonly string m_authToken;

        public TelegramNotificationService(string telegramChatId,
            string authToken)
        {
            m_telegramChatId = telegramChatId;
            m_authToken = authToken;
        }
        
        public void Notify(string body)
        {
            try
            {
                s_logger.LogDebug($"Start send telegram message to {m_telegramChatId}, body: {body}");
                NotifyImpl(body);
                s_logger.LogDebug("Done send telegram message");
            }
            catch (Exception e)
            {
                s_logger.LogError($"Failed send telegram message with body \"{body}\", Error: {e.Message}");
            }
        }

        private void NotifyImpl(string body)
        {
            string urlString = "https://api.telegram.org/bot{0}/sendMessage?chat_id={1}&text={2}";
            string apiToken = m_authToken;
            urlString = String.Format(urlString, apiToken, m_telegramChatId, body);
            using (var httpClient = new HttpClient())
            {
                var res = httpClient.GetAsync(urlString).Result;
                if (res.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception(
                        $"Response from Telegram API: {res.Content.ReadAsStringAsync().Result}");
                }
            }
        }
    }
}