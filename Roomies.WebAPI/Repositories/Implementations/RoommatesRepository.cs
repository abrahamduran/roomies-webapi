using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using Roomies.WebAPI.Models;
using Roomies.WebAPI.Repositories.Interfaces;

namespace Roomies.WebAPI.Repositories.Implementations
{
    public class RoommatesRepository : IRoommatesRepository
    {
        private const string COLLECTION_NAME = "roommates";

        private readonly IMongoCollection<Roommate> _roommates;

        public RoommatesRepository(MongoDBContext context)
        {
            _roommates = context.database.GetCollection<Roommate>(COLLECTION_NAME);

            #region Create Indices
            _roommates.CreateUniqueIndex(x => x.Email);
            _roommates.CreateUniqueIndex(x => x.Username);
            #endregion
        }

        public IEnumerable<Roommate> Get() => _roommates.Find(roommate => true).ToList();

        public Roommate GetById(string id) => _roommates.Find(x => x.Id == id).SingleOrDefault();

        public IEnumerable<Roommate> GetByIds(IEnumerable<string> ids) => _roommates.Find(x => ids.Contains(x.Id)).ToList();

        public Roommate Add(Roommate roommate)
        {
            _roommates.InsertOne(roommate);
            return roommate;
        }

        public decimal UpdateBalance(string id, decimal amount)
        {
            var filter = Builders<Roommate>.Filter.Eq(x => x.Id, id);
            var update = Builders<Roommate>.Update.Inc(x => x.Balance, amount);
            _roommates.UpdateOne(filter, update);

            return _roommates.Find(filter).Project(x => x.Balance).Single();
        }
    }
}
