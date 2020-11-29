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
        public IEnumerable<ExpenseSummary> Expenses { get; set; }

        public static implicit operator PaymentSummary(Payment payment)
        {
            return new PaymentSummary
            {
                Id = payment.Id,
                By = payment.By,
                Date = payment.Date
            };
        }
    }

    public class PaymentSummary
    {
        [BsonElement("_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public DateTime Date { get; set; }
        [BsonElement("_amount")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Amount { get; set; }
        public Payee By { get; set; }
    }

    public class PaymentUpdate
    {
        public string ExpenseId { get; set; }
        public PaymentSummary Summary { get; set; }
    }
}
