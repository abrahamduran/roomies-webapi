using System;

namespace Roomies.WebAPI.Models
{
    public class Payment : Transaction
    {
        public Payee By { get; set; }
        public Payee To { get; set; }
        public Expense.Summary Expense { get; set; }

        private new TransactionType Type { get; }
    }
}
