using Storage.Abstractions.Repository;

namespace Storage.Repository
{
    public class BotResultsRepositoryFactory
    {
        public static IBotResultDetailsRepository Create(string mongoDbHost, string cryptoBotName, string mongoDbDataBase)
        {
            string dataBaseConnectionString = $"mongodb://{mongoDbHost}";
            return new MongoBotResultDetailsRepository(cryptoBotName, dataBaseConnectionString,
                mongoDbDataBase);
        }
    }
}