using System.Collections.Generic;
using System.Linq;

namespace Roomies.WebAPI.Responses
{
    public class RoommateExpenses
    {
        public IEnumerable<ExpenseResult> Expenses { get; set; }
        public decimal YourTotal => Expenses.Sum(x => x.Total.GetValueOrDefault(0));
    }
}
