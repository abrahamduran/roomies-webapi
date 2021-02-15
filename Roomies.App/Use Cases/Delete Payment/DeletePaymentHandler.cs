using System;
using System.Linq;
using Roomies.App.Persistence.Interfaces;

namespace Roomies.App.UseCases.DeletePayment
{
    public class DeletePaymentHandler
    {
        private readonly IPaymentsRepository _payments;
        private readonly IExpensesRepository _expenses;
        private readonly IRoommatesRepository _roommates;

        public DeletePaymentHandler(IPaymentsRepository payments, IExpensesRepository expenses, IRoommatesRepository roommates)
        {
            _payments = payments;
            _expenses = expenses;
            _roommates = roommates;
        }

        public bool? Execute(string id)
        {
            var payment = _payments.Get(id);
            if (payment == null) return null;

            var isRemoved = _payments.Remove(payment);

            if (isRemoved)
            {
                _expenses.UnsetPayment(payment.Id, payment.Expenses);
                _roommates.UpdateBalance(payment.By.Id, payment.Total);
                _roommates.UpdateBalance(payment.To.Id, -payment.Total);
            }

            return isRemoved;
        }
    }
}
