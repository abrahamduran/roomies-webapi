using System;
using System.Collections.Generic;
using Roomies.App.Models;
using Roomies.App.Persistence.Interfaces;

namespace Roomies.Tests.Mocks
{
    public class TransactionsRepositoryMock : ITransactionsRepository
    {
        public IEnumerable<Transaction> Transactions { get; set; }

        public IEnumerable<Transaction> Get() => Transactions;
    }
}
