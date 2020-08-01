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
        public bool NotificationsEnabled { get; set; }

    }
}
