﻿using System;
using Common;
using CryptoBot.Abstractions;
using Infra;
using Microsoft.Extensions.Logging;
using Storage.Abstractions.Providers;

namespace CryptoBot.CryptoValidators
{
    public class RedCandleValidator : ICryptoValidator
    {        
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<RedCandleValidator>();
        private static readonly string s_actionName = "Red Candle Validator";

        private readonly ICurrencyDataProvider m_currencyDataProvider;
        private readonly INotificationService m_notificationService;

        public RedCandleValidator(            
            INotificationService notificationService,
            ICurrencyDataProvider currencyDataProvider)
        {
            m_currencyDataProvider = currencyDataProvider;
            m_notificationService = notificationService;
        }

        public bool Validate(string currency, DateTime currentTime)
        {
            string message;
            MyCandle currCandle = m_currencyDataProvider.GetLastCandle(currency, currentTime);
            if (currCandle.Close < currCandle.Open)
            {
                message = $"{currency} {s_actionName} done, {currCandle} ,{currentTime}";
                m_notificationService.Notify(message);
                s_logger.LogInformation(message);
                return true;
            }

            message = $"{currency} {s_actionName} done, Candle is not red {currCandle} ,{currentTime}";
            m_notificationService.Notify(message);
            s_logger.LogInformation(message);
            return false;
        }
    }
}