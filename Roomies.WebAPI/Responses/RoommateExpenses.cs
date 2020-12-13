using System.Collections.Generic;
using System.Linq;
using Roomies.App.Models;

namespace Roomies.WebAPI.Responses
{
    public class RoommateExpenses
    {
        public IEnumerable<ExpenseResult> Expenses { get; set; }
        public decimal YourTotal => Expenses
            .Where(x => x.Status == ExpenseStatus.Unpaid)
            .Sum(x => x.Total);
    }
}
