using System;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;

namespace Roomies.WebAPI.Repositories
{
    public class MongoDBContext
    {
        private readonly MongoClient _client;
        internal readonly IMongoDatabase database;

        public MongoDBContext(IOptions<RoomiesDBSettings> settings)
        {
            var mongoUrl = new MongoUrl($"{settings.Value.ConnectionString}{settings.Value.DatabaseName}");
            var mongoSettings = MongoClientSettings.FromUrl(mongoUrl);
#if DEBUG
            Console.WriteLine("DEBUG");
            mongoSettings.ClusterConfigurator = cb => {
                cb.Subscribe<CommandStartedEvent>(e => {
                    Console.WriteLine($"{e.CommandName} - {e.Command.ToJson()}");
                });
            };
#endif
            _client = new MongoClient(mongoSettings);
            database = _client.GetDatabase(settings.Value.DatabaseName);
        }
    }
}
