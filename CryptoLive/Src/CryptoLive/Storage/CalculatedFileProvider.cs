using System.IO;

namespace Storage
{
    public class CalculatedFileProvider
    {
        public static string GetCalculatedMacdFile(string currency, int slowEmaSize, int fastEmaSize, int signalSize, string calculatedDataFolder) => Path.Combine(calculatedDataFolder, currency,$"{currency}_Macd_{fastEmaSize}_{slowEmaSize}_{signalSize}_Calculated.csv");

        public static string GetCalculatedEmaAndSignalFile(string currency, int slowEmaSize, int fastEmaSize, int signalSize, string calculatedDataFolder) => Path.Combine(calculatedDataFolder, currency,$"{currency}_EmaAndSignal_{fastEmaSize}_{slowEmaSize}_{signalSize}_Calculated.csv");

        public static string GetCalculatedRsiFile(string currency, int rsiSize, string calculatedDataFolder) => Path.Combine(calculatedDataFolder, currency,$"{currency}_Rsi_{rsiSize}_Calculated.csv");

        public static string GetCalculatedCandleFile(string currency, int candleSize, string calculatedDataFolder) => Path.Combine(calculatedDataFolder, currency,$"{currency}_Candle_{candleSize}_Calculated.csv");

        public static string GetCalculatedWsmaFile(string currency, int rsiSize, string calculatedDataFolder) => Path.Combine(calculatedDataFolder, currency,$"{currency}_Wsma_{rsiSize}_Calculated.csv");
    }
}