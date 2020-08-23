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
                if (x is SimpleExpense)
                    return ((SimpleExpense)x).Payers.Any(p => p.Id == payerId);
                if (x is DetailedExpense)
                    return ((DetailedExpense)x).Items.Any(i => i.Payers.Any(p => p.Id == payerId));
                return false;
            });

        internal static bool ContainsPayee(this IEnumerable<Expense> expenses, string payeeId)
            => expenses.All(x => x.Payee.Id == payeeId);

        internal static decimal TotalForPayer(this IEnumerable<Expense> expenses, string payerId)
            => expenses.Sum(x =>
            {
                if (x is SimpleExpense)
                    return ((SimpleExpense)x).Payers.Single(p => p.Id == payerId).Amount;
                if (x is DetailedExpense)
                    return ((DetailedExpense)x).Items.Sum(i => i.Payers.Single(p => p.Id == payerId).Amount);
                return 0;
            });
    }
}
