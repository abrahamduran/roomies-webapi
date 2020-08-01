using System;
namespace Roomies.WebAPI.Models
{
    public class Expense: Entity
    {
        public decimal Amount { get; set; }
        public ExpenseStatus Status { get; set; }

    }

    public enum ExpenseStatus
    {
        Unpaid, Paid, Declined
    }
}
