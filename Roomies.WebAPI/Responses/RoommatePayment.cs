using System.Collections.Generic;
using Roomies.WebAPI.Models;

namespace Roomies.WebAPI.Responses
{
    public class RoommatePayment
    {
        public PaymentResult Payment { get; set; }
        public decimal YourTotal { get; set; }
    }
}
