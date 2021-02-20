using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using Infra;
using Microsoft.Extensions.Logging;

namespace Utils
{
    public class CsvFileAccess
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<CsvFileAccess>();
        public static readonly string DateTimeFormat =  "dd/MM/yyyy HH:mm:ss";

        public static T[] ReadCsv<T>(string fileName)
        {
            using var reader = new StreamReader(fileName);
            using var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture);
            csvReader.Configuration.HeaderValidated = null;
            csvReader.Configuration.TypeConverterOptionsCache.GetOptions<DateTime>().Formats = new[] {DateTimeFormat};
            var oldData = csvReader.GetRecords<T>();
            return oldData.ToArray();
        }

        public static async Task WriteCsvAsync<T>(string fileName, IEnumerable<T> data)
        {
            s_logger.LogInformation($"Start write new data to {fileName}");
            CreateDirectoryIfNotExist(Path.GetDirectoryName(fileName));
            await using var writer = new StreamWriter(fileName);
            {
                await using var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);
                {
                    csvWriter.Configuration.TypeConverterOptionsCache.GetOptions<DateTime>().Formats = new[] {DateTimeFormat};
                    csvWriter.Configuration.HasHeaderRecord = true;
                    await csvWriter.WriteRecordsAsync((IEnumerable) data.ToArray());
                }
            }
        }
        
        public static void DeleteFile(string fileName)
        {
            s_logger.LogInformation($"Delete file {fileName}");
            File.Delete(fileName);
        }
        
        private static void CreateDirectoryIfNotExist(string folderName)
        {
            if (!Directory.Exists(folderName))
            {
                s_logger.LogInformation($"Create folder {folderName}");
                Directory.CreateDirectory(folderName);
            }
        }
    }
}