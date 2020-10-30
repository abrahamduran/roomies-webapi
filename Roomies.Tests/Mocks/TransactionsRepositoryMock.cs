using System;
using System.Collections.Generic;
using Roomies.WebAPI.Models;
using Roomies.WebAPI.Repositories.Interfaces;

namespace Roomies.Tests.Mocks
{
    public class TransactionsRepositoryMock : ITransactionsRepository
    {
        public IEnumerable<Transaction> Transactions { get; set; }

        public IEnumerable<Transaction> Get() => Transactions;
    }
}
