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
                    return expense.Amount * (decimal)payer.Multiplier;
                case ExpenseDistribution.Even:
                    return expense.Amount / expense.Payers.Count();
                default:
                    return payer.Amount;
            }
        }
    }
}
