using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using Roomies.WebAPI.Models;
using Roomies.WebAPI.Repositories.Interfaces;

namespace Roomies.WebAPI.Repositories.Implementations
{
    public class RoommatesRepository : IRoommatesRepository
    {
        private const string COLLECTION_NAME = "roommates";

        private readonly IMongoCollection<Roommate> _roomies;

        public RoommatesRepository(MongoDBContext context)
        {
            _roomies = context.database.GetCollection<Roommate>(COLLECTION_NAME);

            #region Create Indices
            _roomies.CreateUniqueIndex(x => x.Email);
            _roomies.CreateUniqueIndex(x => x.Username);
            #endregion
        }

        public IEnumerable<Roommate> Get() => _roomies.Find(roomie => true).ToList();

        public Roommate GetById(string id) => _roomies.Find(x => x.Id == id).FirstOrDefault();

        public IEnumerable<Roommate> GetByIds(string[] ids) => _roomies.Find(x => ids.Contains(x.Id)).ToList();

        public Roommate Add(Roommate roomie)
        {
            _roomies.InsertOne(roomie);
            return roomie;
        }
    }
}
