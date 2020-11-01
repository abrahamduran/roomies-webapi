using System;
using Roomies.WebAPI.Models;
using Roomies.WebAPI.Requests;

namespace Roomies.Tests.Mocks
{
    internal static class Mock
    {
        internal static class Models
        {
            internal static SimpleExpense SimpleExpense(
                string id = null, string businessName = "Umbrella Corp", decimal total = 1,
                DateTime? date = null, string description = "Lorem ipsum dolor sit amet",
                ExpenseDistribution distribution = default, Payee payee = null,
                Payer[] payers = null, PaymentSummary[] payments = null)
                => new SimpleExpense
                {
                    Id = id ?? Guid.NewGuid().ToString(),
                    BusinessName = businessName,
                    Date = date ?? DateTime.Now,
                    Description = description,
                    Distribution = distribution,
                    Payee = payee ?? Payee(),
                    Payers = payers ?? new[] { Payer(), Payer(name: "Doctor Foreman") },
                    Payments = payments,
                    Total = total
                };

            internal static DetailedExpense DetailedExpense(
                string id = null, string businessName = "Umbrella Corp", decimal total = 1,
                DateTime? date = null, string description = "Lorem ipsum dolor sit amet",
                Payee payee = null, ExpenseItem[] items = null, PaymentSummary[] payments = null)
                => new DetailedExpense
                {
                    Id = id ?? Guid.NewGuid().ToString(),
                    BusinessName = businessName,
                    Date = date ?? DateTime.Now,
                    Description = description,
                    Payee = payee ?? Payee(),
                    Items = items ?? new[] { ExpenseItem() },
                    Payments = payments,
                    Total = total
                };

            internal static ExpenseSummary ExpenseSummary(
                string id = null, DateTime? date = null, decimal total = 1)
                => new ExpenseSummary
                {
                    Id = id ?? Guid.NewGuid().ToString(),
                    Date = date ?? DateTime.Now,
                    Total = total
                };

            internal static ExpenseItem ExpenseItem(
                int id = 1, string name = "TNT", decimal price = 1, double quantity = 1,
                ExpenseDistribution distribution = default, Payer[] payers = null)
                => new ExpenseItem
                {
                    Id = id,
                    Name = name,
                    Price = price,
                    Quantity = quantity,
                    Distribution = distribution,
                    Payers = payers ?? new[] { Payer(), Payer(name: "Doctor Foreman") }
                };

            internal static Payment Payment(string id = null, Payee by = null, Payee to = null,
                DateTime? date = null, string description = "Lorem ipsum dolor sit amet", decimal total = 2,
                ExpenseSummary[] expenses = null)
                => new Payment
                {
                    Id = id ?? Guid.NewGuid().ToString(),
                    By = by ?? Payee(),
                    To = to ?? Payee(),
                    Date = date ?? DateTime.Now,
                    Description = description,
                    Total = total,
                    Expenses = expenses ?? new[] { ExpenseSummary(), ExpenseSummary() }
                };

            internal static PaymentSummary PaymentSummary(
                string id = null, DateTime? date = null, decimal value = 1,
                Payee by = null)
                => new PaymentSummary
                {
                    Id = id ?? Guid.NewGuid().ToString(),
                    Date = date ?? DateTime.Now,
                    By = by ?? Payee(),
                    Value = value
                };

            internal static Payee Payee(string id = null, string name = "Jack Napier")
                => new Payee { Id = id ?? Guid.NewGuid().ToString(), Name = name };

            internal static Payer Payer(string id = null, string name = "Arthur Wayne", decimal amount = 1)
                => new Payer { Id = id ?? Guid.NewGuid().ToString(), Name = name, Amount = amount };

            internal static Roommate Roommate(
                string id = null, string name = "John Doe", string email = "jdoe@example.com",
                string username = "jdoe", decimal balance = 0, bool notificationsEnabled = true)
                => new Roommate
                {
                    Id = id ?? Guid.NewGuid().ToString(),
                    Balance = balance,
                    Name = name,
                    Email = email,
                    Username = username,
                    NotificationsEnabled = notificationsEnabled
                };
        }

        internal static class Requests
        {
            internal static CreateRoommate Roommate(
                string name = "John Doe", string email = "jdoe@example.com")
                => new CreateRoommate
                {
                    Name = name,
                    Email = email,
                };

            internal static IndexAutocompletableText Autocomplete(
                string text = "Lorem ipsum dolor sit amet", IndexAutocompletableField field = IndexAutocompletableField.ItemName)
                => new IndexAutocompletableText
                {
                    Text = text,
                    Field = field,
                };

            internal static RegisterExpense RegisterSimpleExpense(
                string payeeId = null, string businessName = "Umbrella Corp", decimal total = 1,
                DateTime? date = null, string description = "Lorem ipsum dolor sit amet",
                RegisterExpensePayer[] payers = null, ExpenseDistribution? distribution = ExpenseDistribution.Even)
                => new RegisterExpense
                {
                    BusinessName = businessName,
                    Date = date ?? DateTime.Now,
                    Description = description,
                    Distribution = distribution,
                    PayeeId = payeeId ?? Guid.NewGuid().ToString(),
                    Payers = payers ?? new[] { Payer(), Payer() },
                    Total = total
                };

            internal static RegisterExpense RegisterDetailedExpense(
                string payeeId = null, string businessName = "Umbrella Corp", decimal total = 1,
                DateTime? date = null, string description = "Lorem ipsum dolor sit amet",
                RegisterExpenseItem[] items = null)
                => new RegisterExpense
                {
                    BusinessName = businessName,
                    Date = date ?? DateTime.Now,
                    Description = description,
                    PayeeId = payeeId ?? Guid.NewGuid().ToString(),
                    Items = items ?? new[] { ExpenseItem() },
                    Total = total
                };

            internal static RegisterExpensePayer Payer(string id = null, decimal? amount = null, double? multiplier = null)
                => new RegisterExpensePayer { Id = id ?? Guid.NewGuid().ToString(), Amount = amount, Multiplier = multiplier };

            internal static RegisterExpenseItem ExpenseItem(
                string name = "TNT", ExpenseDistribution distribution = default,
                decimal price = 1, double quantity = 1, RegisterExpensePayer[] payers = null)
                => new RegisterExpenseItem
                {
                    Distribution = distribution,
                    Name = name,
                    Price = price,
                    Quantity = quantity,
                    Payers = payers ?? new[] { Payer(), Payer() }
                };

            internal static RegisterPayment Payment(
                decimal amount = 1, string paidBy = null, string paidTo = null,
                string[] expenseIds = null, string description = "Lorem ipsum dolor sit amet")
                => new RegisterPayment
                {
                    Amount = amount,
                    PaidBy = paidBy ?? Guid.NewGuid().ToString(),
                    PaidTo = paidTo ?? Guid.NewGuid().ToString(),
                    ExpenseIds = expenseIds ?? new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() },
                    Description = description,
                };
        }
    }
}
