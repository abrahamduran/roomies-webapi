using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
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
        public ExpenseStatus Status => Payment != null ? ExpenseStatus.Paid : ExpenseStatus.Unpaid;

        private new TransactionType Type { get; }
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