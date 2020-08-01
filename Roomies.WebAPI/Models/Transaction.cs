using System;
namespace Roomies.WebAPI.Models
{
    public class Transaction : Entity
    {
        public string Description { get; set; }
        public DateTime Date { get; set; }

        private decimal? _payment;

        public decimal? Payment
        {
            get => _payment;
            set
            {
                _payment = value;
                if (_expense != null)
                    _expense = null;
            }
        }

        private decimal? _expense;

        public decimal? Expense
        {
            get => _expense;
            set
            {
                _expense = value;
                if (_payment != null)
                    _payment = null;
            }
        }

        public Status Status
        {
            get => _payment != null ? Status.Paid : Status.Unpaid;
        }
    }

    public enum Status
    {
        Unpaid, Paid
    }
}
