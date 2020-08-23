using System;
using System.ComponentModel.DataAnnotations;

namespace Roomies.WebAPI.Requests
{
    public class RegisterPayment
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least one expense must be selected.")]
        public string[] ExpenseIds { get; set; }
        [Required]
        public string PaidTo { get; set; }
        [Required]
        public string PaidBy { get; set; }
        [Required]
        [Range(1, double.MaxValue, ErrorMessage = "Please enter a valid amount. The {0} field requires values greater than 0.")]
        public decimal Amount { get; set; }
        [MaxLength(100)]
        public string Description { get; set; }
    }
}
