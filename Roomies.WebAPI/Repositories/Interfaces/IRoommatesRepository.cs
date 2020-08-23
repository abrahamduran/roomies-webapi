using System.Collections.Generic;
using Roomies.WebAPI.Models;

namespace Roomies.WebAPI.Repositories.Interfaces
{
    public interface IRoommatesRepository
    {
        Roommate GetById(string id);
        IEnumerable<Roommate> Get();
        IEnumerable<Roommate> GetByIds(IEnumerable<string> ids);
        Roommate Add(Roommate roomie);
        decimal UpdateBalance(string id, decimal amount);
    }
}
