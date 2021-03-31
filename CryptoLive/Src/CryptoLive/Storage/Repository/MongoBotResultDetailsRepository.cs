using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using Infra;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Storage.Abstractions.Repository;
using Storage.MongoDbEntries;

namespace Storage.Repository
{
    public class MongoBotResultDetailsRepository : IBotResultDetailsRepository
    {
        private static readonly ILogger s_logger = ApplicationLogging.CreateLogger<MongoBotResultDetailsRepository>();
        private static readonly string s_collectionName = "BotResultDetails";
        
        private readonly string m_databaseConnectionString;
        private readonly string m_databaseName;
        private readonly string m_cryptoBotName;

        public MongoBotResultDetailsRepository(string cryptoBotName,
            string databaseConnectionString, 
            string databaseName)
        {
            m_cryptoBotName = cryptoBotName;
            m_databaseConnectionString = databaseConnectionString;
            m_databaseName = databaseName;
        }

        public async Task AddAsync(BotResultDetails botResultDetails)
        {
            string description = $"add entry to {s_collectionName}";
            try
            {
                s_logger.LogDebug($"Start {description}");
                await AddImplAsync(botResultDetails);
                s_logger.LogDebug($"Success {description}");
            }
            catch (Exception exception)
            {
                s_logger.LogDebug(exception, $"Failed {description}");
            }
        }

        public async Task<List<BotResultDetails>> GetAllAsync()
        {
            string description = $"get all bot results from {s_collectionName}";
            try
            {
                s_logger.LogDebug($"Start {description}");
                var ans = await GetAllImplAsync();
                s_logger.LogDebug($"Success {description}");
                return ans;
            }
            catch (Exception exception)
            {
                s_logger.LogDebug(exception, $"Failed {description}");
                return new List<BotResultDetails>();
            }
        }
        
        private async Task AddImplAsync(BotResultDetails botResultDetails)
        {
            var db = ConnectToDatabase();
            var collection = db.GetCollection<BotResultDetailsEntry>(s_collectionName);
            var entry = new BotResultDetailsEntry(botResultDetails, m_cryptoBotName);
            await collection.InsertOneAsync(entry);
        }

        private Task<List<BotResultDetails>> GetAllImplAsync()
        {
            var db = ConnectToDatabase();
            var collection = db.GetCollection<BotResultDetailsEntry>(s_collectionName);
            return collection.Find(_=>true).Project(m=> m.BotResultDetails).ToListAsync();        
        }

        private IMongoDatabase ConnectToDatabase()
        {
            var client = new MongoClient(m_databaseConnectionString);
            var database = client.GetDatabase(m_databaseName);
            return database;
        }
    }
}