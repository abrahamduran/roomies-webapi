using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Roomies.WebAPI.Models;
using Roomies.WebAPI.ValidationAttributes;

namespace Roomies.WebAPI.Requests
{
    public class RegisterExpense
    {
        [Required, DataType(DataType.Currency)]
        [Range(1, double.MaxValue, ErrorMessage = "Please enter a valid value. The {0} field requires values greater than 0.")]
        public decimal Total { get; set; }
        [Required, DataType(DataType.Date)]
        public DateTime Date { get; set; }
        [Required]
        public string PayeeId { get; set; }
        [Required, MinLength(1, ErrorMessage = "At least one payer should be selected")]
        public IEnumerable<Payer> Payers { get; set; }
        [MaxLength(100)]
        public string Description { get; set; }
        [Required]
        public ExpenseDistribution Distribution { get; set; }

        public static implicit operator Expense(RegisterExpense registerExpense)
        {
            return new Expense
            {
                Total = registerExpense.Total,
                Date = registerExpense.Date,
                Description = registerExpense.Description,
                Distribution = registerExpense.Distribution
            };
        }

        public class Payer
        {
            [Required]
            public string Id { get; set; }
            [Range(0, double.MaxValue, ErrorMessage = "Please enter a value bigger than {0}.")]
            [DataType(DataType.Currency)]
            public decimal Amount { get; set; }
            [Range(0, double.MaxValue, ErrorMessage = "Please enter a value bigger than {0}.")]
            public double Multiplier { get; set; }
        }
    }
}
