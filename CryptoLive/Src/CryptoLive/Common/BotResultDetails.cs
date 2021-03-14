using System.Collections.Generic;

namespace Common
{
    public class BotResultDetails
    {
        public BotResult BotResult { get; }
        public List<string> PhasesDescription { get; }
        public decimal NewQuoteOrderQuantity { get; }

        public BotResultDetails(BotResult botResult, 
            List<string> phasesDescription, 
            decimal newQuoteOrderQuantity)
        {
            BotResult = botResult;
            PhasesDescription = phasesDescription;
            NewQuoteOrderQuantity = newQuoteOrderQuantity;
        }

        public override string ToString()
        {
            return $"BitResult: {BotResult}, " +
                   $"Phases description: {string.Join(",\n", PhasesDescription)}";
        }
    }
}