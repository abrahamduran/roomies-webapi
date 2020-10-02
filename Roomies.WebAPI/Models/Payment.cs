using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Roomies.WebAPI.Models
{
    public class Payment : Transaction
    {
        public Payee By { get; set; }
        public Payee To { get; set; }
        public IEnumerable<Expense.Summary> Expenses { get; set; }

        public class Summary
        {
            [BsonElement("_id")]
            [BsonRepresentation(BsonType.ObjectId)]
            public string Id { get; set; }
            public DateTime Date { get; set; }
            [BsonElement("_value")]
            [BsonRepresentation(BsonType.Decimal128)]
            public decimal Value { get; set; }
            public Payee By { get; set; }
        }

        public static implicit operator Summary(Payment payment)
        {
            return new Summary
            {
                Id = payment.Id,
                By = payment.By,
                Date = payment.Date
            };
        }
    }
}
