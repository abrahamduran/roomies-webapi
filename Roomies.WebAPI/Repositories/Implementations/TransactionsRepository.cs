using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using Roomies.WebAPI.Models;
using Roomies.WebAPI.Repositories.Interfaces;

namespace Roomies.WebAPI.Repositories.Implementations
{
    public class TransactionsRepository : ITransactionsRepository, IPaymentsRepository, IExpensesRepository
    {
        private const string COLLECTION_NAME = "transactions";

        private readonly IMongoCollection<Transaction> _transactions;
        public TransactionsRepository(MongoDBContext context)
        {
            _transactions = context.database.GetCollection<Transaction>(COLLECTION_NAME);

            #region Create Indices
            _transactions.CreateIndex("type");
            _transactions.CreateIndex(x => x.Date);
            _transactions.OfType<Expense>().CreateIndex(x => x.Payee.Id);
            _transactions.OfType<Expense>().CreateIndex(x => x.BusinessName);
            _transactions.OfType<Expense>().CreateIndex("payers._id");
            _transactions.OfType<DetailedExpense>().CreateIndex("items.name");
            _transactions.OfType<DetailedExpense>().CreateIndex("items.payers._id");
            #endregion
        }

        Expense IExpensesRepository.GetById(string id) => _transactions.OfType<Expense>().Find(x => x.Id == id).Single();

        Payment IPaymentsRepository.GetById(string id) => _transactions.OfType<Payment>().Find(x => x.Id == id).Single();

        IEnumerable<Transaction> ITransactionsRepository.Get() => _transactions.Find(transaction => true).SortByDescending(x => x.Date).ToList();

        IEnumerable<Expense> IExpensesRepository.Get() => _transactions.OfType<Expense>().Find(expense => true).SortByDescending(x => x.Date).ToList();

        IEnumerable<Payment> IPaymentsRepository.Get() => _transactions.OfType<Payment>().Find(payment => true).SortByDescending(x => x.Date).ToList();

        public Expense Add(Expense expense) => (Expense)Register(expense);

        public Payment Add(Payment payment) => (Payment)Register(payment);

        private Transaction Register(Transaction transaction)
        {
            _transactions.InsertOne(transaction);
            return transaction;
        }
    }
}