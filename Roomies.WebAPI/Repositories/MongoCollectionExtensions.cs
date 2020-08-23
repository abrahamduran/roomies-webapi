using System;
using System.Linq.Expressions;
using MongoDB.Driver;

namespace Roomies.WebAPI.Repositories
{
    public static class MongoCollectionExtensions
    {
        public static string CreateIndex<TDocument>(this IMongoCollection<TDocument> collection, Expression<Func<TDocument, object>> field)
        {
            var keys = Builders<TDocument>.IndexKeys.Ascending(field);
            return collection.CreateIndex(keys, new CreateIndexOptions());
        }

        public static string CreateIndex<TDocument>(this IMongoCollection<TDocument> collection, FieldDefinition<TDocument> field)
        {
            var keys = Builders<TDocument>.IndexKeys.Ascending(field);
            return collection.CreateIndex(keys, new CreateIndexOptions());
        }

        public static string CreateUniqueIndex<TDocument>(this IMongoCollection<TDocument> collection, Expression<Func<TDocument, object>> field)
        {
            var keys = Builders<TDocument>.IndexKeys.Ascending(field);
            var options = new CreateIndexOptions() { Unique = true };
            return collection.CreateIndex(keys, options);
        }

        public static string CreateUniqueIndex<TDocument>(this IMongoCollection<TDocument> collection, FieldDefinition<TDocument> field)
        {
            var keys = Builders<TDocument>.IndexKeys.Ascending(field);
            var options = new CreateIndexOptions() { Unique = true };
            return collection.CreateIndex(keys, options);
        }

        private static string CreateIndex<TDocument>(this IMongoCollection<TDocument> collection, IndexKeysDefinition<TDocument> keys, CreateIndexOptions options)
        {
            var model = new CreateIndexModel<TDocument>(keys, options);
            return collection.Indexes.CreateOne(model);
        }
    }
}
