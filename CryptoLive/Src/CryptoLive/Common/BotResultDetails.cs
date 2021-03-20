using System;
using System.Collections.Generic;

namespace Common
{
    public class BotResultDetails
    {
        public BotResult BotResult { get; }
        public List<string> PhasesDescription { get; }
        public decimal NewQuoteOrderQuantity { get; }
        public DateTime EndTime { get; }

        public BotResultDetails(BotResult botResult, 
            List<string> phasesDescription, 
            decimal newQuoteOrderQuantity, 
            DateTime endTime)
        {
            BotResult = botResult;
            PhasesDescription = phasesDescription;
            NewQuoteOrderQuantity = newQuoteOrderQuantity;
            EndTime = endTime;
        }

        public override string ToString()
        {
            return $"BotResult: {BotResult}, Quote order quantity: {NewQuoteOrderQuantity}$, " +
                   $"End time: {EndTime}, \nPhases description: {string.Join(",\n", PhasesDescription)}, ";
        }
    }
}