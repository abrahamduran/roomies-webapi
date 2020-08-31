using System.Collections.Generic;
using System.Linq;
using Roomies.WebAPI.Models;

namespace Roomies.WebAPI.Responses
{
    public class RoommateExpenses
    {
        private IEnumerable<Expense> _expenses;
        public IEnumerable<Expense> Expenses
        {
            get => _expenses;
            set
            {
                _expenses = value.Select(x =>
                {
                    if (x is SimpleExpense simple)
                        simple.Payers = null;
                    else if (x is DetailedExpense detailed)
                        detailed.Items = null;

                    return x;
                });
            }
        }
        public decimal YourTotal { get; set; }
    }
}
