﻿using System;
using System.Collections.Generic;
using Roomies.WebAPI.Models;

namespace Roomies.WebAPI.Repositories.Interfaces
{
    public interface IPaymentsRepository
    {
        Payment GetById(string id);
        IEnumerable<Payment> Get();
        Payment Add(Payment payment);
    }
}