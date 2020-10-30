using System;
using System.Collections.Generic;
using Roomies.WebAPI.Models;

namespace Roomies.WebAPI.Responses
{
    public class ExpenseResult
    {
        public string Id { get; set; }
        public string BusinessName { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public decimal Total { get; set; }
        public Payee Payee { get; set; }

        public ExpenseStatus Status { get; set; }
        public TransactionType Type { get; set; }
        public IEnumerable<PaymentSummary> Payments { get; set; }

        // Simple
        public IEnumerable<Payer> Payers { get; set; }
        public ExpenseDistribution Distribution { get; set; }

        // Detailed
        public IEnumerable<ExpenseItem> Items { get; set; }

        internal static ExpenseResult ForRoommate(Expense expense) =>
            new ExpenseResult
            {
                Id = expense.Id,
                BusinessName = expense.BusinessName,
                Date = expense.Date,
                Description = expense.Description,
                Payee = expense.Payee,
                Payments = expense.Payments,
                Status = expense.Status
            };
    }
}
