﻿using System;
using System.Linq;
using Roomies.App.Models;
using Roomies.WebAPI.Requests;

namespace Roomies.WebAPI.Extensions
{
    internal static class ExpenseDistributionExtension
    {
        internal static decimal GetAmount(this ExpenseDistribution distribution, RegisterExpense expense, RegisterExpensePayer payer)
            => GetAmount(distribution, expense.Total, payer.Amount, payer.Multiplier, expense.Payers.Count());

        internal static decimal GetAmount(this ExpenseDistribution distribution, RegisterExpenseItem item, RegisterExpensePayer payer)
            => GetAmount(distribution, item.Total, payer.Amount, payer.Multiplier, item.Payers.Count());

        private static decimal GetAmount(ExpenseDistribution distribution, decimal total, decimal? payerAmount, double? multiplier, int payersCount)
        {
            switch (distribution)
            {
                case ExpenseDistribution.Proportional:
                    return total * (decimal)multiplier;
                case ExpenseDistribution.Even:
                    return total / payersCount;
                case ExpenseDistribution.Custom:
                    return payerAmount.Value;
            }
            throw new NotImplementedException($"ExpenseDistribution case {distribution} was not properly handled in GetAmount.");
        }
    }
}
