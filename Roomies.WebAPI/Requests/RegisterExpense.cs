using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

        #region Simple Expense
        [MinLength(1, ErrorMessage = "At least one payer must be selected.")]
        public IEnumerable<RegisterExpensePayer> Payers { get; set; }
        public ExpenseDistribution Distribution { get; set; }

        public static implicit operator SimpleExpense(RegisterExpense registerExpense)
        {
            return new SimpleExpense
            {
                Date = registerExpense.Date,
                Total = registerExpense.Total,
                Description = registerExpense.Description,
                BusinessName = registerExpense.BusinessName,
                Distribution = registerExpense.Distribution
            };
        }
        #endregion

        #region Detailed Expense
        [MinLength(1, ErrorMessage = "At least one payer must be selected.")]
        public IEnumerable<RegisterExpenseItem> Items { get; set; }

        public static implicit operator DetailedExpense(RegisterExpense registerExpense)
        {
            return new DetailedExpense
            {
                Date = registerExpense.Date,
                Total = registerExpense.Total,
                Description = registerExpense.Description,
                BusinessName = registerExpense.BusinessName
            };
        }
        #endregion
    }

    public class RegisterExpensePayer
    {
        [Required]
        public string Id { get; set; }
        [DataType(DataType.Currency)]
        [Range(0, double.MaxValue, ErrorMessage = "Please enter a value bigger than {0}.")]
        public decimal Amount { get; set; }
        [Range(0, double.MaxValue, ErrorMessage = "Please enter a value bigger than {0}.")]
        public double Multiplier { get; set; }
    }

    public class RegisterExpenseItem
    {
        [Required, MaxLength(30)]
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

        public decimal Total => Price * (decimal)Quantity;

        public static implicit operator ExpenseItem(RegisterExpenseItem expenseItem)
        {
            return new ExpenseItem
            {
                Name = expenseItem.Name,
                Price = expenseItem.Price,
                Quantity = expenseItem.Quantity,
                Distribution = expenseItem.Distribution
            };
        }
    }
}