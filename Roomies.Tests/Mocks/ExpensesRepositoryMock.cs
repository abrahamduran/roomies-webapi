using System;
using System.Collections.Generic;
using System.Linq;
using Roomies.WebAPI.Models;
using Roomies.WebAPI.Repositories.Interfaces;

namespace Roomies.Tests.Mocks
{
    internal class ExpensesRepositoryMock : IExpensesRepository
    {
        public Expense Expense { get; set; }
        public IEnumerable<Expense> Expenses { get; set; }
        public ExpenseItem ExpenseItem { get; set; }
        public IEnumerable<ExpenseItem> ExpenseItems { get; set; }


        public Expense Add(Expense expense) => expense;

        public Expense Get(string expenseId) => Expense;

        public IEnumerable<Expense> Get() => Expenses;

        public IEnumerable<Expense> Get(IEnumerable<string> expenseIds) => Expenses;

        public IEnumerable<Expense> Get(Roommate roommate) => Expenses;

        public ExpenseItem GetItem(string expenseId, int itemId) => ExpenseItem;

        public IEnumerable<ExpenseItem> GetItems(string expenseId) => ExpenseItems;

        public void SetPayment(IEnumerable<Expense.PaymentUpdate> payments) { }
    }
}
