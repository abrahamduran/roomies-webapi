using System;
using System.Collections.Generic;
using Roomies.WebAPI.Models;

namespace Roomies.WebAPI.Responses
{
    public class RoommatePayments
    {
        public IEnumerable<PaymentResult> Payments { get; set; }
        public decimal YourTotal { get; set; }
    }
}
