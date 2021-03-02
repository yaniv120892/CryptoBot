using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Infra
{
    public static class ApplicationLogging
    {
        private static readonly string s_nlogSettingsFile = "nlog.json";
        public static ILogger CreateLogger<T>() =>
            s_loggerFactory.CreateLogger<T>();

        private static readonly ILoggerFactory s_loggerFactory = LoggerFactory.Create(AddNLog);

        private static void AddNLog(ILoggingBuilder builder)
        {
            try
            {
                var configJson = new ConfigurationBuilder()
                    .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                    .AddJsonFile(s_nlogSettingsFile)
                    .Build();
                var nlogConfig = new NLogLoggingConfiguration(configJson.GetSection("NLog"));
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddNLog(nlogConfig);
            }
            catch (Exception)
            {
                Console.WriteLine("Logging is disabled");
            }
            
        }
    }
}