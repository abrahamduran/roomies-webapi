using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using Roomies.WebAPI.Models;

namespace Roomies.WebAPI.Repositories
{
    public class RoommatesService
    {
        private const string COLLECTION_NAME = "roommates";

        private readonly IMongoCollection<Roommate> _roomies;

        public RoommatesService(IRoomiesDatabaseSettings settings)
        {
            var client = new MongoClient($"{settings.ConnectionString}{settings.DatabaseName}");
            var database = client.GetDatabase(settings.DatabaseName);
            _roomies = database.GetCollection<Roommate>(COLLECTION_NAME);

            #region Create Indices
            _roomies.CreateUniqueIndex(x => x.Email);
            _roomies.CreateUniqueIndex(x => x.Username);
            #endregion
        }

        public IEnumerable<Roommate> Get() => _roomies.Find(roomie => true).ToList();

        public Roommate Add(Roommate roomie)
        {
            _roomies.InsertOne(roomie);
            return roomie;
        }
    }
}
