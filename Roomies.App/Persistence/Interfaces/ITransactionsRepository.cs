using System.Collections.Generic;
using Roomies.App.Models;

namespace Roomies.App.Persistence.Interfaces
{
    public interface ITransactionsRepository
    {
        IEnumerable<Transaction> Get();
    }
}
