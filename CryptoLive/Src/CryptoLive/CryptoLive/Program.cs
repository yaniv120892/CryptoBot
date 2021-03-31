using System;
using System.Threading;
using System.Threading.Tasks;
using CryptoLive.Abstractions;
using Infra;
using Microsoft.Extensions.Logging;
using Storage.Repository;
using Utils.Notifications;

namespace CryptoLive
{
    public class Program
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<Program>();
        private static readonly string s_configFile = "appsettings.json";
        private static IBotListener s_botListener;
        private static ITradingSystem s_tradingSystem;
        private static INotificationService s_notificationService;
        private static CancellationTokenSource s_systemCancellationTokenSource;

        public static async Task Main()
        {
            CryptoLiveParameters appParameters = AppParametersLoader<CryptoLiveParameters>.Load(s_configFile);
            s_systemCancellationTokenSource = new CancellationTokenSource();
            s_notificationService = CreateNotificationService(appParameters);
            s_botListener = CreateBotListener(appParameters);
            s_tradingSystem = CreateTradingSystem(appParameters);
            try
            {
                s_logger.LogInformation($"CryptoLive {appParameters.CryptoBotName} starting...");
                s_botListener.Start();
                Task tradingSystemTask = s_tradingSystem.Run();
                s_notificationService.Notify($"CryptoLive {appParameters.CryptoBotName} started");
                await tradingSystemTask;
            }
            catch (Exception e)
            {
                s_logger.LogError(e, $"CryptoLive {appParameters.CryptoBotName} got exception");
            }
            finally
            {
                s_logger.LogInformation($"CryptoLive {appParameters.CryptoBotName} stopping...");
                s_botListener.Stop();
                s_tradingSystem.Stop();
                s_notificationService.Notify($"CryptoLive {appParameters.CryptoBotName} stopped");
            }
        }

        private static ITradingSystem CreateTradingSystem(CryptoLiveParameters appParameters) => 
            new TradingSystem(s_notificationService, appParameters, s_systemCancellationTokenSource);

        private static IBotListener CreateBotListener(CryptoLiveParameters appParameters)
        {
            var botResultDetailsRepository = BotResultsRepositoryFactory.Create(appParameters.MongoDbHost, 
                    appParameters.CryptoBotName,
                    appParameters.MongoDbDataBase);
            return new TelegramBotListener(botResultDetailsRepository, 
                s_systemCancellationTokenSource,
                appParameters.TelegramAuthToken,
                appParameters.CryptoBotName);
        }

        private static INotificationService CreateNotificationService(CryptoLiveParameters cryptoLiveParameters)
        {
            var notificationServiceFactory =
                new NotificationServiceFactory(cryptoLiveParameters.TwilioWhatsAppSender,
                    cryptoLiveParameters.WhatsAppRecipient, cryptoLiveParameters.TwilioSsid,
                    cryptoLiveParameters.TwilioAuthToken, cryptoLiveParameters.TelegramChatId,
                    cryptoLiveParameters.TelegramAuthToken);
            return notificationServiceFactory.Create(cryptoLiveParameters.NotificationType);
        }
    }
}
