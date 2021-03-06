﻿using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Roomies.App.Models
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
        [BsonElement("_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string Name { get; set; }

        public static bool operator ==(Payee left, Roommate right) => left.Id == right.Id;
        public static bool operator !=(Payee left, Roommate right) => !(left == right);
        public override bool Equals(object obj) => obj is Payee payee && Id == payee.Id;
        public override int GetHashCode() => HashCode.Combine(Id);
    }

    public class Payer
    {
        [BsonElement("_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string Name { get; set; }
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Amount { get; set; }
    }
    #endregion
}
