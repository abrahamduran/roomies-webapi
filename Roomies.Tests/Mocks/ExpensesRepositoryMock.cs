using System;
using System.Collections.Generic;
using System.Linq;
using Roomies.App.Models;
using Roomies.App.Persistence.Interfaces;

namespace Roomies.Tests.Mocks
{
    internal class ExpensesRepositoryMock : IExpensesRepository
    {
        public Expense Expense { get; set; }
        public IEnumerable<Expense> Expenses { get; set; }
        public ExpenseItem ExpenseItem { get; set; }
        public IEnumerable<ExpenseItem> ExpenseItems { get; set; }
        public List<PaymentUpdate> PaymentUpdates { get; set; }
        public bool DeleteResult { get; set; }
        public bool UpdateResult { get; set; }

        public Expense Add(Expense expense) => expense;

        public Expense Get(string expenseId) => Expense;

        public IEnumerable<Expense> Get() => Expenses;

        public IEnumerable<Expense> Get(IEnumerable<string> expenseIds) => Expenses;

        public IEnumerable<Expense> Get(Roommate roommate) => Expenses;

        public ExpenseItem GetItem(string expenseId, int itemId) => ExpenseItem;

        public IEnumerable<ExpenseItem> GetItems(string expenseId) => ExpenseItems;

        public bool Remove(Expense expense) => DeleteResult;

        public bool Update(Expense expense)
        {
            Expense = expense;
            return UpdateResult;
        }

        public void SetPayment(IEnumerable<PaymentUpdate> payments)
        {
            PaymentUpdates = payments.ToList();
            foreach (var update in payments)
            {
                var expense = Expenses.Single(x => x.Id == update.ExpenseId);
                if (expense.Payments == null)
                    expense.Payments = new List<PaymentSummary>();
                ((List<PaymentSummary>)expense.Payments).Add(update.Summary);
            }
        }

        void IExpensesRepository.UnsetPayment(string paymentId, IEnumerable<ExpenseSummary> expenses)
        {
            foreach (var item in expenses)
            {
                var expense = Expenses.Single(x => x.Id == item.Id);
                expense.Payments = null;
            }
        }
    }
}
