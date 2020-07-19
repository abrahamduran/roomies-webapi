using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Roomies.WebAPI.Models
{
    public class Roomie
    {
        [BsonId]
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
