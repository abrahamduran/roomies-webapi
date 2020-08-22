using System.Collections.Generic;
using Roomies.WebAPI.Models;

namespace Roomies.WebAPI.Repositories.Interfaces
{
    public interface ITransactionsRepository
    {
        IEnumerable<Transaction> Get();
    }
}
