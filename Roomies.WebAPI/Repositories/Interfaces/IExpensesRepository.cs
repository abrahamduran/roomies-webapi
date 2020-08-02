using System;
using System.Collections.Generic;
using Roomies.WebAPI.Models;

namespace Roomies.WebAPI.Repositories.Interfaces
{
    public interface IExpensesRepository
    {
        IEnumerable<Expense> GetExpenses();
        Expense Add(Expense expense);
    }
}
