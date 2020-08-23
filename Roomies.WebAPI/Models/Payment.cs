using System;
using System.Collections.Generic;

namespace Roomies.WebAPI.Models
{
    public class Payment : Transaction
    {
        public Payee By { get; set; }
        public Payee To { get; set; }
        public IEnumerable<Expense.Summary> Expenses { get; set; }

        private new TransactionType Type { get; }
    }
}
