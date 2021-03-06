﻿using System.Collections.Generic;
using Roomies.App.Models;

namespace Roomies.App.Persistence.Interfaces
{
    public interface IPaymentsRepository
    {
        Payment Get(string id);
        IEnumerable<Payment> Get();
        IEnumerable<Payment> Get(IEnumerable<string> ids);
        IEnumerable<Payment> Get(Roommate roommate);
        Payment Add(Payment payment);
        bool Remove(Payment payment);
    }
}
