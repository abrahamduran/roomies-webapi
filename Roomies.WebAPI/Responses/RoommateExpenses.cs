using System.Collections.Generic;
using Roomies.WebAPI.Models;

namespace Roomies.WebAPI.Responses
{
    public class RoommateExpenses
    {
        public IEnumerable<Expense> Expenses { get; set; }
        public decimal YourTotal { get; set; }
    }
}
