using System;
using System.Linq.Expressions;
using MongoDB.Driver;

namespace Roomies.WebAPI.Repositories
{
    public static class MongoCollectionExtensions
    {
        public static string CreateUniqueIndex<T>(this IMongoCollection<T> collection, Expression<Func<T, object>> field)
        {
            var keys = Builders<T>.IndexKeys.Ascending(field);
            var options = new CreateIndexOptions() { Unique = true };
            var model = new CreateIndexModel<T>(keys, options);
            return collection.Indexes.CreateOne(model);
        }
    }
}
