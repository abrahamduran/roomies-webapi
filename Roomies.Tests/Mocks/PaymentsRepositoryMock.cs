using System.Collections.Generic;
using Roomies.App.Models;
using Roomies.App.Persistence.Interfaces;

namespace Roomies.Tests.Mocks
{
    internal class PaymentsRepositoryMock : IPaymentsRepository
    {
        public Payment Payment { get; set; }
        public IEnumerable<Payment> Payments { get; set; }
        public bool DeleteResult { get; set; }

        public Payment Add(Payment payment)
        {
            Payment = payment;
            return payment;
        }

        public Payment Get(string id) => Payment;

        public IEnumerable<Payment> Get() => Payments;

        public IEnumerable<Payment> Get(Roommate roommate) => Payments;

        public bool Remove(Payment payment) => DeleteResult;
    }
}
