using System;
using System.Collections.Generic;
using System.Linq;
using Roomies.WebAPI.Models;

namespace Roomies.WebAPI.Extensions
{
    internal static class IEnumberableExpenseExtension
    {
        internal static bool ContainsPayer(this IEnumerable<Expense> expenses, string payerId)
            => expenses.All(x =>
            {
                if (x is SimpleExpense simple)
                    return simple.Payers.Any(p => p.Id == payerId);
                if (x is DetailedExpense detailed)
                    return detailed.Items.Any(i => i.Payers.Any(p => p.Id == payerId));
                return false;
            });

        internal static bool ContainsPayee(this IEnumerable<Expense> expenses, string payeeId)
            => expenses.All(x => x.Payee.Id == payeeId);

        internal static decimal TotalForPayer(this IEnumerable<Expense> expenses, string payerId)
            => expenses.Sum(x => x.TotalForPayer(payerId));

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
