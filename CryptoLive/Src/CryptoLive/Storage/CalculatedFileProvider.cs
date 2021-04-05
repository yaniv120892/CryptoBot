using System.IO;

namespace Storage
{
    public class CalculatedFileProvider
    {
        public static string GetCalculatedRsiFile(string currency, int rsiSize, string calculatedDataFolder) => Path.Combine(calculatedDataFolder, currency,$"{currency}_Rsi_{rsiSize}_Calculated.csv");
        public static string GetCalculatedCandleFile(string currency, string calculatedDataFolder) => Path.Combine(calculatedDataFolder, currency,$"{currency}_Candle_OneMinute_Calculated.csv");
        public static string GetCalculatedWsmaFile(string currency, int rsiSize, string calculatedDataFolder) => Path.Combine(calculatedDataFolder, currency,$"{currency}_Wsma_{rsiSize}_Calculated.csv");
        public static string GetCalculatedMeanAverageFile(string currency, int meanAverageSize, string calculatedDataFolder) => Path.Combine(calculatedDataFolder, currency,$"{currency}_MeanAverage_{meanAverageSize}_Calculated.csv");
    }
}