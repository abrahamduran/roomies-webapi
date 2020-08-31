﻿using System;
using System.Collections.Generic;
using Roomies.WebAPI.Models;

namespace Roomies.WebAPI.Repositories.Interfaces
{
    public interface IExpensesRepository
    {
        Expense Get(string id);
        IEnumerable<Expense> Get();
        IEnumerable<Expense> Get(IEnumerable<string> ids);
        IEnumerable<Expense> Get(Roommate roommate);
        Expense Add(Expense expense);
        ExpenseItem GetItem(string expenseId, int itemId);
        IEnumerable<ExpenseItem> GetItems(string expenseId);
        void SetStatus(IEnumerable<string> ids, ExpenseStatus status);
    }
}
