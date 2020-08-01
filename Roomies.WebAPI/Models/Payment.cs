using System;
namespace Roomies.WebAPI.Models
{
    public class Payment : Entity
    {
        public decimal Amount { get; set; }
        public Expense Expense { get; set; }
    }
}
