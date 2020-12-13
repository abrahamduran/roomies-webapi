using System.Collections.Generic;
using Roomies.App.Models;

namespace Roomies.App.Persistence.Interfaces
{
    public interface IRoommatesRepository
    {
        Roommate Get(string id);
        IEnumerable<Roommate> Get();
        IEnumerable<Roommate> Get(IEnumerable<string> ids);
        Roommate Add(Roommate roommate);
        decimal UpdateBalance(string id, decimal amount);
    }
}
