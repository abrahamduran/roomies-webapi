using System;
using System.Collections.Generic;
using Roomies.WebAPI.Models;

namespace Roomies.WebAPI.Repositories.Interfaces
{
    public interface IExpensesRepository
    {
        Expense GetById(string id);
        IEnumerable<Expense> Get();
        Expense Add(Expense expense);
        ExpenseItem GetItem(string expenseId, int itemId);
        IEnumerable<ExpenseItem> GetItems(string expenseId);
    }
}
