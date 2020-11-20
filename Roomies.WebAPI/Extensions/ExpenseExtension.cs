using System;
using System.Linq;
using Roomies.WebAPI.Models;

namespace Roomies.WebAPI.Extensions
{
    public static class ExpenseExtension
    {
        internal static decimal TotalForPayer(this Expense expense, string payerId)
        {
            if (expense is SimpleExpense simple)
                return simple.Payers.SingleOrDefault(p => p.Id == payerId)?.Amount ?? 0;
            if (expense is DetailedExpense detailed)
                return detailed.Items.Sum(i => i.Payers.SingleOrDefault(p => p.Id == payerId)?.Amount ?? 0);
            return 0;
        }
    }
}
