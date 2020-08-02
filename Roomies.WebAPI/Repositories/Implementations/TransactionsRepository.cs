using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using Roomies.WebAPI.Models;
using Roomies.WebAPI.Repositories.Interfaces;

namespace Roomies.WebAPI.Repositories.Implementations
{
    public class TransactionsRepository: IExpensesRepository
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
            _transactions.OfType<Expense>().CreateIndex("payers._id");
            #endregion
        }

        public IEnumerable<Expense> GetExpenses() => _transactions.OfType<Expense>().Find(expense => true).SortByDescending(x => x.Date).ToList();

        public Expense Add(Expense expense) => (Expense)Register(expense);

        private Transaction Register(Transaction transaction)
        {
            _transactions.InsertOne(transaction);
            return transaction;
        }
    }
}