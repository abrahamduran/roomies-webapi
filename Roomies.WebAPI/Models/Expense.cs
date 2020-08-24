using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Roomies.WebAPI.Models
{
    [BsonDiscriminator(RootClass = true)]
    [BsonKnownTypes(typeof(SimpleExpense), typeof(DetailedExpense))]
    public abstract class Expense : Transaction
    {
        public string BusinessName { get; set; }
        public Payment Payment { get; set; }
        public Payee Payee { get; set; }
        public ExpenseStatus Status => Payment != null ? ExpenseStatus.Paid : ExpenseStatus.Unpaid;

        private new TransactionType Type { get; }

        public class Summary
        {
            [BsonElement("_id")]
            [BsonRepresentation(BsonType.ObjectId)]
            public string Id { get; set; }
            public DateTime Date { get; set; }
            [BsonRepresentation(BsonType.Decimal128)]
            public decimal Total { get; set; }

        }

        public static implicit operator Summary(Expense expense)
        {
            return new Summary
            {
                Id = expense.Id,
                Date = expense.Date,
                Total = expense.Total
            };
        }
    }

    public class SimpleExpense : Expense
    {
        public IEnumerable<Payer> Payers { get; set; }
        public ExpenseDistribution Distribution { get; set; }
    }

    public class DetailedExpense : Expense
    {
        public IEnumerable<ExpenseItem> Items { get; set; }
    }

    public class ExpenseItem
    {
        [BsonElement("_id")]
        public int Id { get; set; }
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Price { get; set; }
        public double Quantity { get; set; }
        public string Name { get; set; }
        public IEnumerable<Payer> Payers { get; set; }
        public ExpenseDistribution Distribution { get; set; }

        public decimal Total => Price * (decimal)Quantity;
    }

    public enum ExpenseStatus
    {
        Unpaid, Paid
    }

    public enum ExpenseDistribution
    {
        Even, Proportional, Custom
    }
}