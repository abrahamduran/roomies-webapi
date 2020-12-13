using System.Collections.Generic;
using Roomies.App.Models;

namespace Roomies.WebAPI.Responses
{
    public class RoommatePayment
    {
        public PaymentResult Payment { get; set; }
        public decimal YourTotal { get; set; }
    }
}
