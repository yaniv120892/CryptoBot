using Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Storage.MongoDbEntries
{
    public class BotResultDetailsEntry
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public BotResultDetails BotResultDetails { get; set; }
        public string CryptoBotName { get; set; }
        
        public BotResultDetailsEntry(BotResultDetails botResultDetails, string cryptoBotName)
        {
            BotResultDetails = botResultDetails;
            CryptoBotName = cryptoBotName;
        }
    }
}