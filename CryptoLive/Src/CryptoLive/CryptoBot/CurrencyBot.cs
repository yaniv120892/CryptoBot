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
        
        private readonly CurrencyBotPhasesExecutor m_currencyBotPhasesExecutor;
        public string Currency { get; }

        public CurrencyBot(
            CurrencyBotPhasesExecutor currencyBotPhasesExecutor, 
            string currency)
        {
            Currency = currency;
            m_currencyBotPhasesExecutor = currencyBotPhasesExecutor;
        }
        
        public async Task<(BotResultDetails, DateTime)> StartAsync(DateTime currentTime)
        {
            int res;
            List<string> phasesDescription;
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            (res, phasesDescription, currentTime) = await StartAsyncImpl(currentTime, cancellationTokenSource, 0);
            BotResult botResult = BotResultConverter.ConvertIntToBotResult(res);
            var botResultDetails = new BotResultDetails(botResult, phasesDescription);
            return (botResultDetails, currentTime);
        }

        private async Task<(int, List<string> , DateTime)> StartAsyncImpl(DateTime currentTime, 
            CancellationTokenSource cancellationTokenSource, 
            int age)
        {
            List<string> phasesDescription = new List<string>();
            int phaseNumber = 0;
            s_logger.LogInformation($"{Currency}_{age}: Start iteration");
            
            //currentTime = await m_currencyBotPhasesExecutor.WaitUntilRsiIsBelowMaxValueAsync(currentTime, cancellationTokenSource.Token, Currency , age, ++phaseNumber, phasesDescription);
            currentTime = await m_currencyBotPhasesExecutor.WaitUntilLowerPriceAndHigherRsiAsync(currentTime, cancellationTokenSource.Token, Currency , age, ++phaseNumber, phasesDescription);

            // Validator
            bool isCandleRed = m_currencyBotPhasesExecutor.ValidateCandleIsRed(currentTime, Currency, age, ++phaseNumber, phasesDescription);
            if(!isCandleRed)
            {
                s_logger.LogInformation($"{Currency}_{age}: Done iteration - candle is not red");
                currentTime = await m_currencyBotPhasesExecutor.WaitAsync(currentTime, cancellationTokenSource.Token, Currency,1*60, "FullMode_WaitAfterCandleNotRed");
                return (0, phasesDescription , currentTime);
            }
            Task<(int,List<string>, DateTime)> child = StartChildAsync(currentTime, cancellationTokenSource, age);
            currentTime = await m_currencyBotPhasesExecutor.WaitAsync(currentTime, cancellationTokenSource.Token, Currency,15*60, "FullMode_WaitAfterCandleIsRed");
            
            bool isCandleGreen = m_currencyBotPhasesExecutor.ValidateCandleIsGreen(currentTime, Currency, age, ++phaseNumber, phasesDescription);
            if(!isCandleGreen)
            {
                s_logger.LogInformation($"{Currency}_{age}: Done iteration - candle is not green");
                return await child;
            }

            cancellationTokenSource.Cancel();
            // Enter
            decimal basePrice = await m_currencyBotPhasesExecutor.GetPriceAsync(Currency, currentTime);
            bool isWin;
            (isWin, currentTime) = await m_currencyBotPhasesExecutor.WaitUnitPriceChangeAsync(currentTime, CancellationToken.None, 
                Currency, basePrice, age, ++phaseNumber, phasesDescription);
            if (isWin)
            {
                s_logger.LogInformation($"{Currency}_{age}: Done iteration - Win {currentTime}");
                return (1, phasesDescription, currentTime);
            }

            s_logger.LogInformation($"{Currency}_{age}: Done iteration - Loss {currentTime}");
            return (-1, phasesDescription, currentTime);
        }
        

        private async Task<(int, List<string> , DateTime)> StartChildAsync(DateTime currentTime, 
            CancellationTokenSource cancellationTokenSource, 
            int age)
        {
            const int timeToWaitInSeconds = 1 * 60;
            s_logger.LogDebug($"{Currency}_{age}: Start child {currentTime}");
            age += 1;
            currentTime = await m_currencyBotPhasesExecutor.WaitAsync(currentTime, cancellationTokenSource.Token,
                Currency, timeToWaitInSeconds, "FullMode_WaitBeforeStartChild");
            return await StartAsyncImpl(currentTime, cancellationTokenSource, age);
        }
    }
}