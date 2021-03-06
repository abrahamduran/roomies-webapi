﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Roomies.WebAPI.Extensions;
using Roomies.App.Models;

namespace Roomies.WebAPI.Responses
{
    public class ExpenseResult : IEquatable<ExpenseResult>
    {
        public string Id { get; set; }
        public string BusinessName { get; set; }
        public string Description { get; set; }
        public long Date { get; set; }
        public decimal Total { get; set; }
        public bool Refundable { get; set; }
        public Payee Payee { get; set; }

        public ExpenseStatus Status { get; set; }
        public IEnumerable<PaymentSummary> Payments { get; set; }

        // Simple
        public ExpenseDistribution? Distribution { get; set; }
        public IEnumerable<Payer> Payers { get; set; }

        // Detailed
        public IEnumerable<ExpenseItem> Items { get; set; }

        public static ExpenseResult ForPayer(Expense expense, string payerId)
        {
            var payerTotal = expense.TotalForPayer(payerId);
            var payment = expense.Payments?.SingleOrDefault(x => x.By.Id == payerId && x.Amount == payerTotal);
            var status = payment != null ? ExpenseStatus.Paid : expense.Status;
            return new ExpenseResult
            {
                Id = expense.Id,
                BusinessName = expense.BusinessName,
                Date = (long)expense.Date.Subtract(DateTime.UnixEpoch).TotalSeconds,
                Description = expense.Description,
                Payee = expense.Payee,
                Status = status,
                Total = payerTotal
            };
        }

        public static ExpenseResult ForExpense(Expense expense, bool includePayments)
        {
            if (expense is SimpleExpense simple) return ForSimpleExpense(simple, includePayments);
            if (expense is DetailedExpense detailed) return ForDetailedExpense(detailed, includePayments);
            return null;
        }

        public static ExpenseResult ForSimpleExpense(SimpleExpense expense, bool includePayments) =>
            new ExpenseResult
            {
                BusinessName = expense.BusinessName,
                Date = (long)expense.Date.Subtract(DateTime.UnixEpoch).TotalSeconds,
                Description = expense.Description,
                Distribution = expense.Distribution,
                Id = expense.Id,
                Payee = expense.Payee,
                Payers = expense.Payers,
                Payments = includePayments ? expense.Payments : null,
                Refundable = expense.Refundable,
                Status = expense.Status,
                Total = expense.Total
            };

        public static ExpenseResult ForDetailedExpense(DetailedExpense expense, bool includePayments) =>
            new ExpenseResult
            {
                BusinessName = expense.BusinessName,
                Date = (long)expense.Date.Subtract(DateTime.UnixEpoch).TotalSeconds,
                Description = expense.Description,
                Id = expense.Id,
                Items = expense.Items,
                Payee = expense.Payee,
                Payments = includePayments ? expense.Payments : null,
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
            Description == other?.Description &&
            BusinessName == other?.BusinessName &&
            Distribution == other?.Distribution;
    }
}
