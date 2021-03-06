﻿using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Roomies.App.Models
{
    [BsonKnownTypes(typeof(Payment), typeof(Expense))]
    public abstract class Transaction : Entity
    {
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime Date { get; set; }
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Total { get; set; }
        public string Description { get; set; }
        public TransactionType Type => this is Expense ? TransactionType.Expense : TransactionType.Payment;
    }

    public enum TransactionType
    {
        Payment, Expense
    }
}
