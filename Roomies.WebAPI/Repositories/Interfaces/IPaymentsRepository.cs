using System;
using System.Collections.Generic;
using Roomies.WebAPI.Models;

namespace Roomies.WebAPI.Repositories.Interfaces
{
    public interface IPaymentsRepository
    {
        Payment Get(string id);
        IEnumerable<Payment> Get();
        IEnumerable<Payment> Get(Roommate roommate);
        Payment Add(Payment payment);
    }
}
