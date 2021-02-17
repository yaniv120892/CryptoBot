using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Infra;
using Microsoft.Extensions.Logging;
using Utils.Converters;

namespace CryptoBot
{
    public class CurrencyBot
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<CurrencyBot>();
        
        private readonly ICurrencyBotPhasesExecutor m_currencyBotPhasesExecutor;
        private readonly CancellationTokenSource m_cancellationTokenSource;
        private readonly int m_age;
        private DateTime m_currentTime;
        public string Currency { get; }

        public CurrencyBot(
            ICurrencyBotPhasesExecutor currencyBotPhasesExecutor,
            string currency,
            CancellationTokenSource cancellationTokenSource,
            DateTime botStartTime,
            int age = 0)
        {
            Currency = currency;
            m_cancellationTokenSource = cancellationTokenSource;
            m_currentTime = botStartTime;
            m_age = age;
            m_currencyBotPhasesExecutor = currencyBotPhasesExecutor;
        }
        
        public async Task<(BotResultDetails, DateTime)> StartAsync()
        {
            int res;
            List<string> phasesDescription;
            (res, phasesDescription) = await StartAsyncImpl();
            BotResult botResult = BotResultConverter.ConvertIntToBotResult(res);
            var botResultDetails = new BotResultDetails(botResult, phasesDescription);
            return (botResultDetails, m_currentTime);
        }

        private async Task<(int, List<string>)> StartAsyncImpl()
        {
            List<string> phasesDescription = new List<string>();
            int phaseNumber = 0;
            s_logger.LogInformation($"{Currency}_{m_age}: Start iteration");
            
            //currentTime = await m_currencyBotPhasesExecutor.WaitUntilRsiIsBelowMaxValueAsync(currentTime, cancellationTokenSource.Token, Currency , age, ++phaseNumber, phasesDescription);
            m_currentTime = await m_currencyBotPhasesExecutor.WaitUntilLowerPriceAndHigherRsiAsync(m_currentTime, m_cancellationTokenSource.Token, Currency , m_age, ++phaseNumber, phasesDescription);

            // Validator
            bool isCandleRed = m_currencyBotPhasesExecutor.ValidateCandleIsRed(m_currentTime, Currency, m_age, ++phaseNumber, phasesDescription);
            if(!isCandleRed)
            {
                s_logger.LogInformation($"{Currency}_{m_age}: Done iteration - candle is not red");
                m_currentTime = await m_currencyBotPhasesExecutor.WaitAsync(m_currentTime, m_cancellationTokenSource.Token, Currency,1*60, "FullMode_WaitAfterCandleNotRed");
                return (0, phasesDescription);
            }
            Task<(int,List<string>)> child = StartChildAsync();
            m_currentTime = await m_currencyBotPhasesExecutor.WaitAsync(m_currentTime, m_cancellationTokenSource.Token, Currency,15*60, "FullMode_WaitAfterCandleIsRed");
            
            bool isCandleGreen = m_currencyBotPhasesExecutor.ValidateCandleIsGreen(m_currentTime, Currency, m_age, ++phaseNumber, phasesDescription);
            if(!isCandleGreen)
            {
                s_logger.LogInformation($"{Currency}_{m_age}: Done iteration - candle is not green");
                return await child;
            }

            m_cancellationTokenSource.Cancel();
            // Enter
            decimal basePrice = await m_currencyBotPhasesExecutor.GetPriceAsync(Currency, m_currentTime);
            bool isWin;
            (isWin, m_currentTime) = await m_currencyBotPhasesExecutor.WaitUnitPriceChangeAsync(m_currentTime, CancellationToken.None, 
                Currency, basePrice, m_age, ++phaseNumber, phasesDescription);
            if (isWin)
            {
                s_logger.LogInformation($"{Currency}_{m_age}: Done iteration - Win {m_currentTime}");
                return (1, phasesDescription);
            }

            s_logger.LogInformation($"{Currency}_{m_age}: Done iteration - Loss {m_currentTime}");
            return (-1, phasesDescription);
        }
        

        private async Task<(int, List<string>)> StartChildAsync()
        {
            const int timeToWaitInSeconds = 1 * 60;
            s_logger.LogDebug($"{Currency}_{m_age}: Start child {m_currentTime}");
            m_currentTime = await m_currencyBotPhasesExecutor.WaitAsync(m_currentTime, m_cancellationTokenSource.Token,
                Currency, timeToWaitInSeconds, "FullMode_WaitBeforeStartChild");
            var childCurrencyBot = new CurrencyBot(m_currencyBotPhasesExecutor, Currency, m_cancellationTokenSource, m_currentTime, m_age+1);
            return await childCurrencyBot.StartAsyncImpl();
        }
    }
}