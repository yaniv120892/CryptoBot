using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using CryptoLive.Abstractions;
using Infra;
using Microsoft.Extensions.Logging;
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

        private readonly CancellationTokenSource m_systemCancellationTokenSource;
        private readonly string m_cryptoBotName;
        private readonly TelegramBotClient m_bot;
        private readonly ConcurrentDictionary<string, List<BotResultDetails>> m_mapCurrencyToBotResultDetails;

        private DateTime m_startTime;


        public TelegramBotListener(string token, 
            CancellationTokenSource systemCancellationTokenSource,
            string cryptoBotName,
            string[] currencies)
        {
            m_systemCancellationTokenSource = systemCancellationTokenSource;
            m_cryptoBotName = cryptoBotName;
            m_bot = new TelegramBotClient(token);
            m_mapCurrencyToBotResultDetails = new ConcurrentDictionary<string, List<BotResultDetails>>();
            foreach (var currency in currencies)
            {
                m_mapCurrencyToBotResultDetails[currency] = new List<BotResultDetails>();
            }
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

        public void AddResults(string currency, BotResultDetails botResultDetails)
        {
            m_mapCurrencyToBotResultDetails[currency].Add(botResultDetails);
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
            string replyBody = CalculateBotResults();
            await m_bot.SendTextMessageAsync(
                chatId: chatId,
                text: replyBody);
        }

        private string CalculateBotResults()
        {
            string botResults = "Results:\n";
            int totalWin = 0;
            int totalLoss = 0;
            foreach (var currency in m_mapCurrencyToBotResultDetails.Keys)
            {
                int winAmount = m_mapCurrencyToBotResultDetails[currency]
                    .Count(m => m.BotResult.Equals(BotResult.Win));
                int lossAmount = m_mapCurrencyToBotResultDetails[currency]
                    .Count(m => m.BotResult.Equals(BotResult.Win));
                string currencyResult = $"{currency} Win: {winAmount}, Loss: {lossAmount}\n";
                botResults += currencyResult;
                totalWin += winAmount;
                totalLoss += lossAmount;
            }

            botResults += $"Summary Win:{totalWin}, Loss: {totalLoss}";
            return botResults;
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