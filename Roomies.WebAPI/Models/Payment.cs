using System;
namespace Roomies.WebAPI.Models
{
    public class Payment : Transaction
    {
        public Expense Expense { get; set; }

        private new TransactionType Type { get; }
    }
}
