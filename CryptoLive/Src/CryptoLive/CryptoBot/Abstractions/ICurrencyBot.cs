using System;
using System.Threading.Tasks;
using Common;

namespace CryptoBot.Abstractions
{
    public interface ICurrencyBot
    {
        Task<(BotResultDetails, DateTime)> StartAsync();
    }
}