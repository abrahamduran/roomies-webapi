using Roomies.WebAPI.Models;

namespace Roomies.WebAPI.Responses
{
    public class RoommateExpense
    {
        public Expense Expense { get; set; }
        public decimal YourTotal { get; set; }
    }
}
