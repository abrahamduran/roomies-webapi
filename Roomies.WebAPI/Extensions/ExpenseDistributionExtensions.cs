using System.Linq;
using Roomies.WebAPI.Models;
using Roomies.WebAPI.Requests;

namespace Roomies.WebAPI.Extensions
{
    internal static class ExpenseDistributionExtensions
    {
        internal static decimal GetAmount(this ExpenseDistribution distribution, RegisterExpense expense, RegisterExpense.Payer payer)
        {
            switch (distribution)
            {
                case ExpenseDistribution.Proportional:
                    return expense.Total * (decimal)payer.Multiplier;
                case ExpenseDistribution.Even:
                    return expense.Total / expense.Payers.Count();
                default:
                    return payer.Amount;
            }
        }
    }
}
