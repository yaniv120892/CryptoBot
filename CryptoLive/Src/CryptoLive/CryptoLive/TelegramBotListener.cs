using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using CryptoLive.Abstractions;
using Infra;
using Microsoft.Extensions.Logging;
using Storage.Abstractions.Repository;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CryptoLive
{
    public class TelegramBotListener : IBotListener
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<TelegramBotListener>();

        private readonly IBotResultDetailsRepository m_botResultDetailsRepository;
        private readonly CancellationTokenSource m_systemCancellationTokenSource;
        private readonly string m_cryptoBotName;
        private readonly TelegramBotClient m_bot;

        private DateTime m_startTime;
        
        public TelegramBotListener(IBotResultDetailsRepository botResultDetailsRepository,
            CancellationTokenSource systemCancellationTokenSource,
            string token,
            string cryptoBotName)
        {
            m_systemCancellationTokenSource = systemCancellationTokenSource;
            m_cryptoBotName = cryptoBotName;
            m_botResultDetailsRepository = botResultDetailsRepository;
            m_bot = new TelegramBotClient(token);
        }

        public void Start()
        {
            m_startTime = DateTime.UtcNow;
            m_bot.OnMessage += BotOnMessageReceived;
            m_bot.OnReceiveError += BotOnReceiveError;
            m_bot.StartReceiving();
        }
        
        public void Stop()
        {
            m_systemCancellationTokenSource.Cancel();
        }

        private async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;
            if (ShouldIgnore(message))
                return;

            string[] messageWords = message.Text.Split(' ');
            string chatId = message.Chat.Id.ToString();
            switch (messageWords.FirstOrDefault())
            {
                // Send inline keyboard
                case "/status":
                    if (messageWords.Length == 2 && messageWords[1].Equals(m_cryptoBotName))
                    {
                        await SendBotStatus(chatId);
                    }
                    break;
                case "/stop":
                    if (messageWords.Length == 2 && messageWords[1].Equals(m_cryptoBotName))
                    {
                        await StopBot(chatId);
                    }
                    break;
                case "/results":
                    if (messageWords.Length == 2 && messageWords[1].Equals(m_cryptoBotName))
                    {
                        await SendBotResults(chatId);
                    }
                    break;
                default:
                    await Usage(message);
                    break;
            }
        }

        private bool ShouldIgnore(Message message) => 
            message == null || message.Type != MessageType.Text || message.Date < m_startTime;

        private async Task SendBotResults(string chatId)
        {
            await m_bot.SendTextMessageAsync(
                chatId: chatId,
                text: "Processing results...");
            string replyBody = await CalculateBotResults();
            await m_bot.SendTextMessageAsync(
                chatId: chatId,
                text: replyBody);
        }

        private async Task<string> CalculateBotResults()
        {
            Dictionary<string, List<BotResultDetails>> mapCurrencyToBotResultDetails = await GetMappingCurrencyToBotResultDetails();

            int totalWin = 0;
            int totalLoss = 0;
            string botResultDescription = "Results:\n";
            foreach (string currency in mapCurrencyToBotResultDetails.Keys)
            {
                int winAmount = mapCurrencyToBotResultDetails[currency]
                    .Count(m => m.BotResult.Equals(BotResult.Win));
                int lossAmount = mapCurrencyToBotResultDetails[currency]
                    .Count(m => m.BotResult.Equals(BotResult.Loss));
                string currencyResult = $"{currency} Win: {winAmount}, Loss: {lossAmount}\n";
                botResultDescription += currencyResult;
                totalWin += winAmount;
                totalLoss += lossAmount;
            }

            botResultDescription += $"Summary Win:{totalWin}, Loss: {totalLoss}";
            return botResultDescription;
        }

        private async Task<Dictionary<string, List<BotResultDetails>>> GetMappingCurrencyToBotResultDetails()
        {
            List<BotResultDetails> botResults = await m_botResultDetailsRepository.GetAllAsync();
            var mapCurrencyToBotResultDetails = new Dictionary<string, List<BotResultDetails>>();
            foreach (var botResult in botResults)
            {
                if (!mapCurrencyToBotResultDetails.ContainsKey(botResult.Currency))
                {
                    mapCurrencyToBotResultDetails[botResult.Currency] = new List<BotResultDetails>();
                }

                mapCurrencyToBotResultDetails[botResult.Currency].Add(botResult);
            }

            return mapCurrencyToBotResultDetails;
        }

        private async Task StopBot(string chatId)
        {
            await m_bot.SendChatActionAsync(chatId, ChatAction.Typing);
            const string replyBody = "Stopping...";
            await m_bot.SendTextMessageAsync(
                chatId: chatId,
                text: replyBody);
            m_bot.StopReceiving();
            Stop();
        }

        private async Task SendBotStatus(string chatId)
        {
            await m_bot.SendChatActionAsync(chatId, ChatAction.Typing);
            var replyBody = m_systemCancellationTokenSource.IsCancellationRequested ? "Not running" : "Running";

            await m_bot.SendTextMessageAsync(
                chatId: chatId,
                text: replyBody);
        }

        private async Task Usage(Message message)
        {
            const string usage = "Usage:\n" +
                                 "/status {CryptoBotName} - get system status\n" +
                                 "/stop {CryptoBotName} - send stop system request\n" +
                                 "/results {CryptoBotName} - get win/loss summary\n" +
                                 "";
            await m_bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: usage,
                replyMarkup: new ReplyKeyboardRemove());
        }
        
        private static void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs)
        {
            s_logger.LogError("Received error: {0} — {1}",
                receiveErrorEventArgs.ApiRequestException.ErrorCode,
                receiveErrorEventArgs.ApiRequestException.Message
            );
        }
    }
}