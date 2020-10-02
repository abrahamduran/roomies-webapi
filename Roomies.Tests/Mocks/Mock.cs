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
                string id = null, string businessName = "Umbrella Corp",
                DateTime? date = null, string description = "Lorem ipsum dolor sit amet",
                ExpenseDistribution distribution = default, Payee payee = null,
                Payer[] payers = null, ExpenseStatus status = default, decimal total = 1)
            {
                return new SimpleExpense
                {
                    Id = id ?? Guid.NewGuid().ToString(),
                    BusinessName = businessName,
                    Date = date ?? DateTime.Now,
                    Description = description,
                    Distribution = distribution,
                    Payee = payee ?? Payee(),
                    Payers = payers ?? new[] { Payer(), Payer(name: "Doctor Foreman") },
                    Total = total
                };
            }

            internal static DetailedExpense DetailedExpense(
                string id = null, string businessName = "Umbrella Corp",
                DateTime? date = null, string description = "Lorem ipsum dolor sit amet",
                Payee payee = null, ExpenseItem[] items = null,
                ExpenseStatus status = default, decimal total = 1)
            {
                return new DetailedExpense
                {
                    Id = id ?? Guid.NewGuid().ToString(),
                    BusinessName = businessName,
                    Date = date ?? DateTime.Now,
                    Description = description,
                    Payee = payee ?? Payee(),
                    Items = items ?? new[] { ExpenseItem() },
                    Total = total
                };
            }

            internal static ExpenseItem ExpenseItem(
                int id = 1, string name = "TNT", decimal price = 1, double quantity = 1,
                ExpenseDistribution distribution = default, Payer[] payers = null)
            {
                return new ExpenseItem
                {
                    Id = id,
                    Name = name,
                    Price = price,
                    Quantity = quantity,
                    Distribution = distribution,
                    Payers = payers ?? new[] { Payer(), Payer(name: "Doctor Foreman") }
                };
            }

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
            internal static RegisterExpense RegisterSimpleExpense(
                string payeeId = null, string businessName = "Umbrella Corp", decimal total = 1,
                DateTime? date = null, string description = "Lorem ipsum dolor sit amet",
                RegisterExpensePayer[] payers = null, ExpenseDistribution distribution = default)
            {
                return new RegisterExpense
                {
                    BusinessName = businessName,
                    Date = date ?? DateTime.Now,
                    Description = description,
                    Distribution = distribution,
                    PayeeId = payeeId ?? Guid.NewGuid().ToString(),
                    Payers = payers ?? new[] { Payer(), Payer() },
                    Total = total
                };
            }

            internal static RegisterExpense RegisterDetailedExpense(
                string payeeId = null, string businessName = "Umbrella Corp", decimal total = 1,
                DateTime? date = null, string description = "Lorem ipsum dolor sit amet",
                RegisterExpenseItem[] items = null)
            {
                return new RegisterExpense
                {
                    BusinessName = businessName,
                    Date = date ?? DateTime.Now,
                    Description = description,
                    PayeeId = payeeId ?? Guid.NewGuid().ToString(),
                    Items = items ?? new[] { ExpenseItem() },
                    Total = total
                };
            }

            internal static RegisterExpensePayer Payer(string id = null, decimal? amount = null, double? multiplier = null)
                => new RegisterExpensePayer { Id = id ?? Guid.NewGuid().ToString(), Amount = amount, Multiplier = multiplier };

            internal static RegisterExpenseItem ExpenseItem(
                string name = "TNT", ExpenseDistribution distribution = default,
                decimal price = 1, double quantity = 1, RegisterExpensePayer[] payers = null)
            {
                return new RegisterExpenseItem
                {
                    Distribution = distribution,
                    Name = name,
                    Price = price,
                    Quantity = quantity,
                    Payers = payers ?? new[] { Payer(), Payer() }
                };
            }

            internal static object RegisterSimpleExpense(object[] payers)
            {
                throw new NotImplementedException();
            }
        }
    }
}
