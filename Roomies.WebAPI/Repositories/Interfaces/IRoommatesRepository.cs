using System.Collections.Generic;
using Roomies.WebAPI.Models;

namespace Roomies.WebAPI.Repositories.Interfaces
{
    public interface IRoommatesRepository
    {
        IEnumerable<Roommate> Get();
        Roommate GetById(string id);
        IEnumerable<Roommate> GetByIds(string[] ids);
        Roommate Add(Roommate roomie);
    }
}
