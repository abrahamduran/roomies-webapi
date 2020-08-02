using System.Collections.Generic;
using Roomies.WebAPI.Models;

namespace Roomies.WebAPI.Repositories.Interfaces
{
    public interface IRoommatesRepository
    {
        IEnumerable<Roommate> Get();
        Roommate GetById(string id);
        IEnumerable<Roommate> GetByIds(IEnumerable<string> ids);
        Roommate Add(Roommate roomie);
        decimal UpdateBalance(string id, decimal amount);
    }
}
