using System;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;

namespace Roomies.App.Persistence
{
    public class MongoDBContext
    {
        private readonly MongoClient _client;
        internal readonly IMongoDatabase database;

        public MongoDBContext(IOptions<RoomiesDBSettings> settings) : this(settings.Value) { }

        public MongoDBContext(RoomiesDBSettings settings)
        {
            var mongoUrl = new MongoUrl($"{settings.ConnectionString}{settings.DatabaseName}");
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
            database = _client.GetDatabase(settings.DatabaseName);
        }
    }
}
