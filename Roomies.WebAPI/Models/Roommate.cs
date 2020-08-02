using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Roomies.WebAPI.Models
{
    public class Roommate : Entity
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Balance { get; set; }
        public bool NotificationsEnabled { get; set; }
    }

    #region Payee & Payer
    public class Payee
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class Payer
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string Name { get; set; }
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Amount { get; set; }
    }
    #endregion
}
