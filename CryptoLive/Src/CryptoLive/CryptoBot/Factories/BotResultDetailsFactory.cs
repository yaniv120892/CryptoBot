using System;
using System.Collections.Generic;
using Common;
using Common.Abstractions;

namespace CryptoBot.Factories
{
    public static class BotResultDetailsFactory
    {
        internal static BotResultDetails CreateFailureBotResultDetails(PollingResponseBase pollingResponseBase)
        {
            if (pollingResponseBase.IsCancelled)
            {
                return CreateFailureBotResultDetails(pollingResponseBase.Time, new OperationCanceledException());
            }

            if (pollingResponseBase.Exception != null)
            {
                return CreateFailureBotResultDetails(pollingResponseBase.Time, pollingResponseBase.Exception);
            }

            throw new Exception("Polling finish with failure but did not got cancellation request or exception");
        }
        
        internal static BotResultDetails CreateFailureBotResultDetails(DateTime botEndTime, Exception exception) => 
            new BotResultDetails(BotResult.Faulted, botEndTime, exception);
        
        internal static BotResultDetails CreateSuccessBotResultDetails(BotResult botResult, 
            DateTime botEndTime, 
            List<string> phasesDescription)
        {
            if (botResult.Equals(BotResult.Win) 
                || botResult.Equals(BotResult.Loss))
            {
                return new BotResultDetails(botResult, phasesDescription, botEndTime);
            }
            
            throw new Exception($"BotResult should be Win, Loss or Even but was {botResult}");
        }
    }
}