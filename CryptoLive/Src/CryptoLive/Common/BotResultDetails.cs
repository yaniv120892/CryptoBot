using System.Collections.Generic;

namespace Common
{
    public class BotResultDetails
    {
        public BotResult BotResult { get; }
        public List<string> PhasesDescription { get; }
        
        public BotResultDetails(BotResult botResult, List<string> phasesDescription)
        {
            BotResult = botResult;
            PhasesDescription = phasesDescription;
        }
    }
}