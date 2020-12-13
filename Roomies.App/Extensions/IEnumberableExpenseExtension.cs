using System.Collections.Generic;
using System.Linq;
using Roomies.App.Models;

namespace Roomies.App.Extensions
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
    }
}
