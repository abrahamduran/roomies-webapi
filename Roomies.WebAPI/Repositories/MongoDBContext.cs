using System;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Roomies.WebAPI.Repositories
{
    public class MongoDBContext
    {
        private readonly MongoClient client;
        internal readonly IMongoDatabase database;

        public MongoDBContext(IOptions<RoomiesDBSettings> settings)
        {
            client = new MongoClient($"{settings.Value.ConnectionString}{settings.Value.DatabaseName}");
            database = client.GetDatabase(settings.Value.DatabaseName);
        }
    }
}
