using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Roomies.WebAPI.Models;

namespace Roomies.WebAPI.Responses
{
    public class PaymentResult : IEquatable<PaymentResult>
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public long Date { get; set; }
        public decimal Total { get; set; }
        public Payee By { get; set; }
        public Payee To { get; set; }
        public IEnumerable<ExpenseSummary> Expenses { get; set; }

        public static PaymentResult ForRoommate(Payment payment) =>
            new PaymentResult
            {
                Id = payment.Id,
                Date = (long)payment.Date.Subtract(DateTime.UnixEpoch).TotalSeconds,
                Description = payment.Description,
                Expenses = payment.Expenses,
                To = payment.To,
                Total = payment.Total
            };

        public static PaymentResult ForPayment(Payment payment, bool includeExpenses) =>
            new PaymentResult
            {
                By = payment.By,
                Id = payment.Id,
                Date = (long)payment.Date.Subtract(DateTime.UnixEpoch).TotalSeconds,
                Description = payment.Description,
                Expenses = includeExpenses ? payment.Expenses : null,
                To = payment.To,
                Total = payment.Total
            };

        public bool Equals([AllowNull] PaymentResult other) =>
            By == other?.By &&
            Id == other?.Id &&
            Date == other?.Date &&
            Description == other?.Description &&
            Expenses == other?.Expenses &&
            To == other?.To &&
            Total == other?.Total;
    }
}
