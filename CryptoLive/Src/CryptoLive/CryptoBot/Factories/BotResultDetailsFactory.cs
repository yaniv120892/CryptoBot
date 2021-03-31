using System;
using System.Collections.Generic;
using Common;
using Common.Abstractions;

namespace CryptoBot.Factories
{
    public static class BotResultDetailsFactory
    {
        internal static BotResultDetails CreateFailureBotResultDetails(PollingResponseBase pollingResponseBase, 
            string currency)
        {
            if (pollingResponseBase.IsCancelled)
            {
                return CreateFailureBotResultDetails(pollingResponseBase.Time, new OperationCanceledException(), currency);
            }

            if (pollingResponseBase.Exception != null)
            {
                return CreateFailureBotResultDetails(pollingResponseBase.Time, pollingResponseBase.Exception, currency);
            }

            throw new Exception("Polling finish with failure but did not got cancellation request or exception");
        }
        
        internal static BotResultDetails CreateFailureBotResultDetails(DateTime botEndTime, Exception exception,
            string currency) => 
            new BotResultDetails(BotResult.Faulted, botEndTime, exception, currency);
        
        internal static BotResultDetails CreateSuccessBotResultDetails(BotResult botResult, 
            DateTime botEndTime, 
            List<string> phasesDescription,
            string currency)
        {
            if (botResult.Equals(BotResult.Win) 
                || botResult.Equals(BotResult.Loss))
            {
                return new BotResultDetails(botResult, phasesDescription, botEndTime, currency);
            }
            
            throw new Exception($"BotResult should be Win, Loss or Even but was {botResult}");
        }
    }
}