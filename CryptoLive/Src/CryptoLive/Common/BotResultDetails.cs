using System;
using System.Collections.Generic;

namespace Common
{
    public class BotResultDetails
    {
        public BotResult BotResult { get; }
        public List<string> PhasesDescription { get; }
        public DateTime EndTime { get; }

        public BotResultDetails(BotResult botResult, 
            List<string> phasesDescription, 
            DateTime endTime)
        {
            BotResult = botResult;
            PhasesDescription = phasesDescription;
            EndTime = endTime;
        }

        public override string ToString()
        {
            return $"BotResult: {BotResult}, " +
                   $"End time: {EndTime}, \nPhases description: {string.Join(",\n", PhasesDescription)}, ";
        }
    }
}