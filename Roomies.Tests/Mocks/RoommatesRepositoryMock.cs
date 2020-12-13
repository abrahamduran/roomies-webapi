using System;
using System.Collections.Generic;
using System.Linq;
using Roomies.App.Models;
using Roomies.App.Persistence.Interfaces;

namespace Roomies.Tests.Mocks
{
    public class RoommatesRepositoryMock : IRoommatesRepository
    {
        public Roommate Roommate { get; set; }
        public IEnumerable<Roommate> Roommates { get; set; }


        public Roommate Add(Roommate roommate)
        {
            Roommate = roommate;
            return roommate;
        }

        public Roommate Get(string id) => Roommate;

        public IEnumerable<Roommate> Get() => Roommates;

        public IEnumerable<Roommate> Get(IEnumerable<string> ids) => Roommates;

        public decimal UpdateBalance(string id, decimal amount)
        {
            var roommate = Roommates.SingleOrDefault(x => x.Id == id) ?? Roommate;
            if (roommate.Id != id) throw new ArgumentException();

            roommate.Balance += amount;
            return roommate.Balance;
        }
    }
}
