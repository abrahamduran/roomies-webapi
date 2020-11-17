using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Roomies.WebAPI.Models;

namespace Roomies.WebAPI.Responses
{
    public class ExpenseResult : IEquatable<ExpenseResult>
    {
        public string Id { get; set; }
        public string BusinessName { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public decimal Total { get; set; }
        public Payee Payee { get; set; }

        public ExpenseStatus Status { get; set; }
        public IEnumerable<PaymentSummary> Payments { get; set; }

        // Simple
        public ExpenseDistribution? Distribution { get; set; }
        public IEnumerable<Payer> Payers { get; set; }

        // Detailed
        public IEnumerable<ExpenseItem> Items { get; set; }

        public static ExpenseResult ForRoommate(Expense expense) =>
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

        public static ExpenseResult ForExpense(Expense expense)
        {
            if (expense is SimpleExpense simple) return ForSimpleExpense(simple);
            if (expense is DetailedExpense detailed) return ForDetailedExpense(detailed);
            return null;
        }

        public static ExpenseResult ForSimpleExpense(SimpleExpense expense) =>
            new ExpenseResult
            {
                BusinessName = expense.BusinessName,
                Date = expense.Date,
                Description = expense.Description,
                Distribution = expense.Distribution,
                Id = expense.Id,
                Payee = expense.Payee,
                Payers = expense.Payers,
                Status = expense.Status,
                Total = expense.Total
            };

        public static ExpenseResult ForDetailedExpense(DetailedExpense expense) =>
            new ExpenseResult
            {
                BusinessName = expense.BusinessName,
                Date = expense.Date,
                Description = expense.Description,
                Id = expense.Id,
                Items = expense.Items,
                Payee = expense.Payee,
                Status = expense.Status,
                Total = expense.Total
            };

        public bool Equals([AllowNull] ExpenseResult other) =>
            Id == other?.Id &&
            Date == other?.Date &&
            Items == other?.Items &&
            Payee == other?.Payee &&
            Total == other?.Total &&
            Status == other?.Status &&
            Payers == other?.Payers &&
            Payments == other?.Payments &&
            Description == other.Description &&
            BusinessName == other.BusinessName &&
            Distribution == other?.Distribution;
    }
}
