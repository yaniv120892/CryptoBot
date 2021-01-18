using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infra
{
    public class AppParametersLoader<T>
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<AppParametersLoader<T>>();

        public static T Load(string configFileName)
        {
            object appParameters = LoadFromConfigFile(configFileName);
            if (appParameters is T parameters)
            {
                return parameters;
            }
            throw new Exception($"Failed to load app parameters from config file {configFileName}, " +
                                $"please make sure AppParameters section exists in config file {configFileName}");
        }

        private static object LoadFromConfigFile(string configFileName)
        {
            s_logger.LogDebug($"Start load app parameters from config file {configFileName}");
            IConfigurationSection appParametersSection = GetAppParametersSection(configFileName);
            ConstructorInfo ctor = typeof(T).GetConstructor(new[] { typeof(IConfigurationSection) });
            if (ctor == null)
            {
                return null;
            }
            
            object appParameters = ctor.Invoke(new object[] { appParametersSection });
            Console.WriteLine($"Done load tool parameters from config file {configFileName}");
            return appParameters;
        }

        private static IConfigurationSection GetAppParametersSection(string configFileName)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(configFileName)
                .Build();
            
            var appParametersSection = builder.GetSection("AppParameters");
            return appParametersSection;
        }
    }
}