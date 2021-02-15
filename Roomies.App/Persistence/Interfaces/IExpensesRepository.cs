using System.Collections.Generic;
using Roomies.App.Models;

namespace Roomies.App.Persistence.Interfaces
{
    public interface IExpensesRepository
    {
        Expense Get(string expenseId);
        IEnumerable<Expense> Get();
        IEnumerable<Expense> Get(IEnumerable<string> ids);
        IEnumerable<Expense> Get(Roommate roommate);
        Expense Add(Expense expense);
        bool Remove(Expense expense);
        bool Update(Expense expense);
        ExpenseItem GetItem(string expenseId, int itemId);
        IEnumerable<ExpenseItem> GetItems(string expenseId);
        void SetPayment(IEnumerable<PaymentUpdate> payments);
        void UnsetPayment(string paymentId, IEnumerable<ExpenseSummary> expenses);
    }
}
