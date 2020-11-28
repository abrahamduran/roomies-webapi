using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Roomies.WebAPI.Models;

namespace Roomies.WebAPI.Requests
{
    public class RegisterExpense
    {
        [Required, DataType(DataType.Currency)]
        [Range(1, double.MaxValue, ErrorMessage = "Please enter a valid value. The {0} field requires values greater than 0.")]
        public decimal Total { get; set; }
        [Required, DataType(DataType.Date)]
        public DateTime Date { get; set; }
        [Required, MaxLength(30)]
        public string BusinessName { get; set; }
        [Required]
        public string PayeeId { get; set; }
        [MaxLength(100)]
        public string Description { get; set; }
        public IEnumerable<string> Tags { get; set; }

        #region Simple Expense
        [MinLength(1, ErrorMessage = "At least one payer must be selected.")]
        public IEnumerable<RegisterExpensePayer> Payers { get; set; }
        public ExpenseDistribution? Distribution { get; set; }
        public bool? Refundable { get; set; }

        public static implicit operator SimpleExpense(RegisterExpense registerExpense)
        {
            return new SimpleExpense
            {
                Date = registerExpense.Date,
                Tags = registerExpense.Tags,
                Total = registerExpense.Total,
                Description = registerExpense.Description,
                BusinessName = registerExpense.BusinessName,
                Refundable = registerExpense.Refundable.Value,
                Distribution = registerExpense.Distribution.Value
            };
        }
        #endregion

        #region Detailed Expense
        [MinLength(2, ErrorMessage = "At least two items must be added.")]
        public IEnumerable<RegisterExpenseItem> Items { get; set; }

        public static implicit operator DetailedExpense(RegisterExpense registerExpense)
        {
            return new DetailedExpense
            {
                Date = registerExpense.Date,
                Tags = registerExpense.Tags,
                Total = registerExpense.Total,
                Description = registerExpense.Description,
                BusinessName = registerExpense.BusinessName
            };
        }
        #endregion

        public static RegisterExpense From(Expense expense)
        {
            if (expense is SimpleExpense simple)
                return From(simple);
            if (expense is DetailedExpense detailed)
                return From(detailed);
            return null;

        }

        public static RegisterExpense From(SimpleExpense simple)
        {
            return new RegisterExpense
            {
                BusinessName = simple.BusinessName,
                Date = simple.Date,
                Description = simple.Description,
                Distribution = simple.Distribution,
                PayeeId = simple.Payee.Id,
                Payers = simple.Payers.Select(x => RegisterExpensePayer.From(x, simple.Distribution, simple.Total)).ToList(),
                Refundable = simple.Refundable,
                Total = simple.Total,
                Tags = simple.Tags
            };
        }

        public static RegisterExpense From(DetailedExpense detailed)
        {
            return new RegisterExpense
            {
                BusinessName = detailed.BusinessName,
                Date = detailed.Date,
                Description = detailed.Description,
                PayeeId = detailed.Payee.Id,
                Items = detailed.Items.Select(i => RegisterExpenseItem.From(i)).ToList(),
                Total = detailed.Total,
                Tags = detailed.Tags
            };
        }
    }

    public class RegisterExpensePayer
    {
        [Required]
        public string Id { get; set; }
        [DataType(DataType.Currency)]
        [Range(0, double.MaxValue, ErrorMessage = "Please enter a value bigger than {0}.")]
        public decimal? Amount { get; set; }
        [Range(0, 1, ErrorMessage = "Please enter a value between 0 and 1.")]
        public double? Multiplier { get; set; }

        public static RegisterExpensePayer From(Payer payer, ExpenseDistribution distribution, decimal total)
        {
            (decimal? amount, double? multiplier) = (null, null);
            switch (distribution)
            {
                case ExpenseDistribution.Custom: amount = payer.Amount; break;
                case ExpenseDistribution.Proportional: multiplier = (double)(payer.Amount / total); break;
                default: break;
            }
            return new RegisterExpensePayer
            {
                Id = payer.Id,
                Amount = amount,
                Multiplier = multiplier
            };
        }
    }

    public class RegisterExpenseItem
    {
        [Required, MaxLength(40)]
        public string Name { get; set; }
        [Required, DataType(DataType.Currency)]
        [Range(0.1, double.MaxValue, ErrorMessage = "Please enter a value bigger than {0}.")]
        public decimal Price { get; set; }
        [Range(0.1, double.MaxValue, ErrorMessage = "Please enter a value bigger than {0}.")]
        public double Quantity { get; set; }
        [Required, MinLength(1, ErrorMessage = "At least one payer should be selected.")]
        public IEnumerable<RegisterExpensePayer> Payers { get; set; }
        [Required]
        public ExpenseDistribution Distribution { get; set; }
        public bool Refundable { get; set; }

        public decimal Total => Price * (decimal)Quantity;

        public static implicit operator ExpenseItem(RegisterExpenseItem expenseItem)
        {
            return new ExpenseItem
            {
                Name = expenseItem.Name,
                Price = expenseItem.Price,
                Quantity = expenseItem.Quantity,
                Refundable = expenseItem.Refundable,
                Distribution = expenseItem.Distribution
            };
        }

        public static RegisterExpenseItem From(ExpenseItem item)
        {
            return new RegisterExpenseItem
            {
                Name = item.Name,
                Price = item.Price,
                Quantity = item.Quantity,
                Refundable = item.Refundable,
                Distribution = item.Distribution,
                Payers = item.Payers.Select(x => RegisterExpensePayer.From(x, item.Distribution, item.Total)).ToList()
            };
        }
    }
}