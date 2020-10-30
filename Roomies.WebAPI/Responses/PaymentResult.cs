using System;
using System.Collections.Generic;
using Roomies.WebAPI.Models;

namespace Roomies.WebAPI.Responses
{
    public class PaymentResult
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public decimal Total { get; set; }
        public Payee By { get; set; }
        public Payee To { get; set; }
        public IEnumerable<Expense.Summary> Expenses { get; set; }

        internal static PaymentResult ForRoommate(Payment payment) =>
            new PaymentResult
            {
                Id = payment.Id,
                Date = payment.Date,
                Description = payment.Description,
                To = payment.To,
                Expenses = payment.Expenses
            };
    }
}
