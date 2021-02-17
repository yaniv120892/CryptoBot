using System.Collections.Generic;
using System.Text;

namespace DemoCryptoLive
{
    internal class TestResultsSummary
    {
        internal static string BuildWinAndLossDescriptions(List<List<string>> lossesPhaseDetails, List<List<string>> winPhaseDetails)
        {
            StringBuilder lossesDescription = new StringBuilder();
            lossesDescription.AppendLine("Losses phases details:");
            for (int i = 0; i < lossesPhaseDetails.Count; i++)
            {
                lossesDescription.AppendLine($"Loss {i}:");
                foreach (string phase in lossesPhaseDetails[i])
                {
                    lossesDescription.AppendLine(phase);
                }

                lossesDescription.AppendLine();
            }

            StringBuilder winsDescription = new StringBuilder();
            winsDescription.AppendLine("Wins phases details:");
            for (int i = 0; i < winPhaseDetails.Count; i++)
            {
                winsDescription.AppendLine($"Win {i}:");
                foreach (string phase in winPhaseDetails[i])
                {
                    winsDescription.AppendLine(phase);
                }

                winsDescription.AppendLine();
            }

            string winAndLossDescriptions = $"{winsDescription}\n{lossesDescription}";
            return winAndLossDescriptions;
        }
    }
}