using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Roomies.WebAPI.Repositories
{
    public class MongoDBContext
    {
        private readonly MongoClient _client;
        internal readonly IMongoDatabase database;

        public MongoDBContext(IOptions<RoomiesDBSettings> settings)
        {
            _client = new MongoClient($"{settings.Value.ConnectionString}{settings.Value.DatabaseName}");
            database = _client.GetDatabase(settings.Value.DatabaseName);
        }
    }
}
