using System;
using System.Linq;
using Roomies.App.Extensions;
using Roomies.App.Models;
using Roomies.App.Persistence.Interfaces;

namespace Roomies.App.UseCases.RegisterPayment
{
    public class RegisterPaymentHandler
    {
        private const decimal MAX_OFFSET_VALUE = 0.1M;

        private readonly IPaymentsRepository _payments;
        private readonly IExpensesRepository _expenses;
        private readonly IRoommatesRepository _roommates;

        public RegisterPaymentHandler(IPaymentsRepository payments, IExpensesRepository expenses, IRoommatesRepository roommates)
        {
            _payments = payments;
            _expenses = expenses;
            _roommates = roommates;
        }

        public RegisterPaymentResponse Execute(RegisterPaymentRequest payment)
        {
            var roommates = _roommates.Get(new[] { payment.PaidTo, payment.PaidBy }).ToDictionary(x => x.Id);

            #region Validations
            var exception = new UseCaseException();
            if (!roommates.ContainsKey(payment.PaidBy))
                exception.AddError("PaidBy", "The PaidBy field is invalid. Please review it.");
            if (!roommates.ContainsKey(payment.PaidTo))
                exception.AddError("PaidTo", "The PaidTo field is invalid. Please review it.");

            if (payment.PaidBy == payment.PaidTo)
            {
                exception.AddError("PaidBy", "The PaidBy field appears to be identical to the PaidTo. Please review it.");
                exception.AddError("PaidTo", "The PaidTo field appears to be identical to the PaidBy. Please review it.");
            }

            var expenses = _expenses.Get(payment.ExpenseIds);
            if (expenses.Count() != payment.ExpenseIds.Count() || !expenses.Any())
            {
                exception.AddError("ExpenseIds", "At least one expense is invalid. Please review them before submission.");
                foreach (var expense in expenses.Where(x => !payment.ExpenseIds.Contains(x.Id)).ToList())
                    exception.AddError("ExpenseIds", $"Invalid expense: {expense.Id}");
            }

            if (expenses.Any(x => x.Status == ExpenseStatus.Paid) || expenses.Any(x => x.Payments?.Any(p => p.By.Id == payment.PaidBy) ?? false))
            {
                exception.AddError("ExpenseIds", "At least one expense has already been paid. Please review them before submission.");
                foreach (var expense in expenses.Where(x => x.Status == ExpenseStatus.Paid).ToList())
                    exception.AddError("ExpenseIds", $"Paid expense: {expense.Id}");
            }

            if (exception.Errors.Any()) throw exception;

            if (!expenses.ContainsPayer(payment.PaidBy))
            {
                exception.AddError("PaidBy", "The selected payer is invalid for the selected expenses.");
                exception.AddError("ExpenseIds", "At least one expense does not contains the selected payer.");
                foreach (var expense in expenses.Where(x => x.Status == ExpenseStatus.Paid).ToList())
                    exception.AddError("ExpenseIds", $"Paid expense: {expense.Id}");
            }
            if (!expenses.ContainsPayee(payment.PaidTo))
            {
                exception.AddError("PaidTo", "The selected payee is invalid for the selected expenses.");
                exception.AddError("ExpenseIds", "At least one expense does not contains the selected payee.");
            }

            if (exception.Errors.Any()) throw exception;

            var totalExpense = expenses.TotalForPayer(payment.PaidBy);
            if (payment.Amount < totalExpense || payment.Amount > (totalExpense + MAX_OFFSET_VALUE))
            {
                exception.AddError("Amount", "The amount introduced does not match with the total amount for the selected expenses.");
                exception.AddError("Amount", "As of now, partial payments are not supported.");
                exception.AddError("Amount", $"Payment amount: {payment.Amount}, expenses total: {totalExpense}, difference: {totalExpense - payment.Amount}");
                throw exception;
            }
            #endregion

            var entity = new Payment
            {
                By = new Payee { Id = payment.PaidBy, Name = roommates[payment.PaidBy].Name },
                To = new Payee { Id = payment.PaidTo, Name = roommates[payment.PaidTo].Name },
                Expenses = expenses.Select(x => (ExpenseSummary)x).ToList(),
                Description = payment.Description,
                Total = payment.Amount,
                Date = DateTime.Now
            };

            var result = _payments.Add(entity);
            if (result == null) throw exception;

            var payments = expenses.Select(x => {
                var summary = (PaymentSummary)result;
                summary.Amount = x.TotalForPayer(payment.PaidBy);
                return new PaymentUpdate { ExpenseId = x.Id, Summary = summary };
            }).ToList();
            _roommates.UpdateBalance(payment.PaidBy, -payment.Amount);
            _roommates.UpdateBalance(payment.PaidTo, payment.Amount);
            _expenses.SetPayment(payments);
            return toResponse(result, false);
        }

        private RegisterPaymentResponse toResponse(Payment payment, bool includesExpenses)
            => RegisterPaymentResponse.ForPayment(payment, includesExpenses);
    }
}
