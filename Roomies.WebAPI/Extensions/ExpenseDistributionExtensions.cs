using System.Linq;
using Roomies.WebAPI.Models;
using Roomies.WebAPI.Requests;

namespace Roomies.WebAPI.Extensions
{
    internal static class ExpenseDistributionExtensions
    {
        internal static decimal GetAmount(this ExpenseDistribution distribution, RegisterExpense expense, RegisterExpensePayer payer)
            => GetAmount(distribution, expense.Total, payer.Multiplier, expense.Payers.Count(), payer.Amount);

        internal static decimal GetAmount(this ExpenseDistribution distribution, RegisterExpenseItem item, RegisterExpensePayer payer)
            => GetAmount(distribution, item.Total, payer.Multiplier, item.Payers.Count(), payer.Amount);

        private static decimal GetAmount(ExpenseDistribution distribution, decimal total, double multiplier, int payersCount, decimal payerAmount)
        {
            switch (distribution)
            {
                case ExpenseDistribution.Proportional:
                    return total * (decimal)multiplier;
                case ExpenseDistribution.Even:
                    return total / payersCount;
                default:
                    return payerAmount;
            }
        }
    }
}
