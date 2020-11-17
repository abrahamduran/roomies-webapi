using System;
using System.Collections.Generic;
using Roomies.WebAPI.Models;

namespace Roomies.WebAPI.Responses
{
    public class PaymentResult
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public long Date { get; set; }
        public decimal Total { get; set; }
        public Payee By { get; set; }
        public Payee To { get; set; }
        public IEnumerable<ExpenseSummary> Expenses { get; set; }

        internal static PaymentResult ForRoommate(Payment payment) =>
            new PaymentResult
            {
                Id = payment.Id,
                Date = (long)payment.Date.Subtract(DateTime.UnixEpoch).TotalSeconds,
                Description = payment.Description,
                To = payment.To,
                Expenses = payment.Expenses
            };
    }
}
