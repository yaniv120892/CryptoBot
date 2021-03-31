using System;
using System.Collections.Generic;

namespace Common
{
    public class BotResultDetails
    {
        public string Currency { get; }
        public BotResult BotResult { get; }
        public List<string> PhasesDescription { get; }
        public DateTime EndTime { get; }
        public Exception Exception { get; }

        public BotResultDetails(BotResult botResult, 
            List<string> phasesDescription, 
            DateTime endTime, 
            string currency)
        {
            BotResult = botResult;
            PhasesDescription = phasesDescription;
            EndTime = endTime;
            Currency = currency;
            Exception = null;
        }
        
        public BotResultDetails(BotResult botResult,
            DateTime endTime, Exception exception, 
            string currency)
        {
            BotResult = botResult;
            PhasesDescription = new List<string>();
            EndTime = endTime;
            Exception = exception;
            Currency = currency;
        }

        public override string ToString()
        {
            return $"Currency: {Currency}, " +
                   $"BotResult: {BotResult}, " +
                   $"End time: {EndTime}, " +
                   $"Exception: {Exception?.Message}" +
                   $"\nPhases description: {string.Join(",\n", PhasesDescription)}, ";
        }
    }
}