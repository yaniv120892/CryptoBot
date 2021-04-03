using System;
using System.Collections.Generic;

namespace Common
{
    public class BotResultDetails
    {
        public BotResult BotResult { get; }
        public List<string> PhasesDescription { get; }
        public DateTime EndTime { get; }
        public Exception Exception { get; }

        public BotResultDetails(BotResult botResult, 
            List<string> phasesDescription, 
            DateTime endTime)
        {
            BotResult = botResult;
            PhasesDescription = phasesDescription;
            EndTime = endTime;
            Exception = null;
        }
        
        public BotResultDetails(BotResult botResult,
            DateTime endTime, Exception exception)
        {
            BotResult = botResult;
            PhasesDescription = new List<string>();
            EndTime = endTime;
            Exception = exception;
        }

        public override string ToString()
        {
            return $"BotResult: {BotResult}, " +
                   $"End time: {EndTime:dd/MM/yyyy HH:mm:ss}, " +
                   $"Exception: {Exception?.Message}" +
                   $"\nPhases description: {string.Join(",\n", PhasesDescription)}, ";
        }
    }
}