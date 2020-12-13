using System;
using System.Collections.Generic;
using Roomies.App.Models;

namespace Roomies.WebAPI.Responses
{
    public class RoommatePayments
    {
        public IEnumerable<PaymentResult> Payments { get; set; }
        public decimal YourTotal { get; set; }
    }
}
