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
            var options = new CreateIndexOptions();
            var model = new CreateIndexModel<TDocument>(keys, options);
            return collection.Indexes.CreateOne(model);
        }

        public static string CreateIndex<TDocument>(this IMongoCollection<TDocument> collection, FieldDefinition<TDocument> field)
        {

            var keys = Builders<TDocument>.IndexKeys.Ascending(field);
            var options = new CreateIndexOptions();
            var model = new CreateIndexModel<TDocument>(keys, options);
            return collection.Indexes.CreateOne(model);
        }

        public static string CreateUniqueIndex<TDocument>(this IMongoCollection<TDocument> collection, Expression<Func<TDocument, object>> field)
        {
            var keys = Builders<TDocument>.IndexKeys.Ascending(field);
            var options = new CreateIndexOptions() { Unique = true };
            var model = new CreateIndexModel<TDocument>(keys, options);
            return collection.Indexes.CreateOne(model);
        }
    }
}
