using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Roomies.WebAPI.Models
{
    public class Expense: Transaction
    {
        public Payment Payment { get; set; }
        public ExpenseStatus Status => Payment != null ? ExpenseStatus.Paid : ExpenseStatus.Unpaid;
        public IEnumerable<Payer> Payers { get; set; }
        public ExpenseDistribution Distribution { get; set; }

        private new TransactionType Type { get; }
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