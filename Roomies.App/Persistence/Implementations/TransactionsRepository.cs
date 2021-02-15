using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using Roomies.App.Models;
using Roomies.App.Persistence.Interfaces;

namespace Roomies.App.Persistence.Implementations
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
            _transactions.OfType<Payment>().CreateIndex(x => x.By.Id);
            _transactions.OfType<Payment>().CreateIndex(x => x.To.Id);
            _transactions.OfType<Payment>().CreateIndex("expenses._id");
            _transactions.OfType<Expense>().CreateIndex(x => x.Tags);
            _transactions.OfType<Expense>().CreateIndex(x => x.Payee.Id);
            _transactions.OfType<Expense>().CreateIndex("payers._id");
            _transactions.OfType<DetailedExpense>().CreateIndex("items._id");
            _transactions.OfType<DetailedExpense>().CreateIndex("items.payers._id");
            #endregion
        }

        Expense IExpensesRepository.Get(string id) => GetById<Expense>(id);

        Payment IPaymentsRepository.Get(string id) => GetById<Payment>(id);

        IEnumerable<Transaction> ITransactionsRepository.Get() => GetList<Transaction>();

        IEnumerable<Expense> IExpensesRepository.Get() => GetList<Expense>();

        IEnumerable<Payment> IPaymentsRepository.Get() => GetList<Payment>();

        IEnumerable<Expense> IExpensesRepository.Get(IEnumerable<string> ids) => GetByIds<Expense>(ids);

        IEnumerable<Payment> IPaymentsRepository.Get(IEnumerable<string> ids) => GetByIds<Payment>(ids);

        IEnumerable<Expense> IExpensesRepository.Get(Roommate roommate)
        {
            var roommateId = ObjectId.Parse(roommate.Id);
            var filterPayee = Builders<Expense>.Filter.Ne("payee._id", roommateId);
            var filterPayer = Builders<Expense>.Filter.Or(new[]
            {
                Builders<Expense>.Filter.Eq("payers._id", roommateId),
                Builders<Expense>.Filter.Eq("items.payers._id", roommateId)
            });

            return _transactions.OfType<Expense>().Find(filterPayee & filterPayer).SortByDescending(x => x.Date).ToList();
        }

        IEnumerable<Payment> IPaymentsRepository.Get(Roommate roommate)
        {
            var roommateId = ObjectId.Parse(roommate.Id);
            var filter = Builders<Payment>.Filter.Eq(x => x.By.Id, roommate.Id);

            return _transactions.OfType<Payment>().Find(filter).SortByDescending(x => x.Date).ToList();
        }

        ExpenseItem IExpensesRepository.GetItem(string expenseId, int itemId)
        {
            //var filterExpenses = Builders<DetailedExpense>.Filter.Eq(x => x.Id, expenseId);
            //var filterItems = Builders<DetailedExpense>.Filter.ElemMatch(x => x.Items, x => x.Id == itemId);
            //return _transactions.OfType<DetailedExpense>().Find(filterExpenses & filterItems).Project(x => x.Items).Single().SingleOrDefault();
            var query = from expense in _transactions.OfType<DetailedExpense>().AsQueryable()
                        where expense.Id == expenseId
                        select expense.Items.Where(x => x.Id == itemId);
            return query.ToList().Single().SingleOrDefault();
        }

        IEnumerable<ExpenseItem> IExpensesRepository.GetItems(string expenseId)
        {
            var expense = _transactions.OfType<DetailedExpense>().Find(x => x.Id == expenseId).SingleOrDefault();
            return expense?.Items.OrderBy(x => x.Id);
        }

        void IExpensesRepository.SetPayment(IEnumerable<PaymentUpdate> payments)
        {
            var updates = payments.Select(x =>
            {
                var filter = Builders<Expense>.Filter.Eq(x => x.Id, x.ExpenseId);
                var update = Builders<Expense>.Update.AddToSet(x => x.Payments, x.Summary);
                return new UpdateOneModel<Expense>(filter, update);
            }).ToList();
            _transactions.OfType<Expense>().BulkWrite(updates);
        }

        void IExpensesRepository.UnsetPayment(string paymentId, IEnumerable<ExpenseSummary> expenses)
        {
            var updates = expenses.Select(x =>
            {
                var filter = Builders<Expense>.Filter.Eq(x => x.Id, x.Id);
                var update = Builders<Expense>.Update.PullFilter(x => x.Payments, x => x.Id == paymentId);
                return new UpdateOneModel<Expense>(filter, update);
            }).ToList();
            _transactions.OfType<Expense>().BulkWrite(updates);
        }

        Expense IExpensesRepository.Add(Expense expense) => (Expense)Register(expense);

        Payment IPaymentsRepository.Add(Payment payment) => (Payment)Register(payment);

        private Transaction Register(Transaction transaction)
        {
            _transactions.InsertOne(transaction);
            return transaction;
        }

        bool IExpensesRepository.Remove(Expense expense) => Remove<Expense>(expense.Id);

        bool IPaymentsRepository.Remove(Payment payment) => Remove<Payment>(payment.Id);

        bool IExpensesRepository.Update(Expense expense)
            => _transactions.OfType<Expense>().ReplaceOne(x => x.Id == expense.Id, expense).ModifiedCount > 0;

        #region Generics
        private IEnumerable<Type> GetList<Type>() where Type : Transaction
        {
            try
            {
                return _transactions.OfType<Type>().Find(transaction => true).SortByDescending(x => x.Date).ToList();
            }
            catch
            {
                return null;
            }
        }

        private Type GetById<Type>(string id) where Type : Transaction
        {
            try
            {
                return _transactions.OfType<Type>().Find(x => x.Id == id).SingleOrDefault();
            }
            catch
            {
                return null;
            }
        }

        private IEnumerable<Type> GetByIds<Type>(IEnumerable<string> ids) where Type : Transaction
        {
            try
            {
                return _transactions.OfType<Type>().Find(x => ids.Contains(x.Id)).SortByDescending(x => x.Date).ToList();
            }
            catch
            {
                return null;
            }
        }

        private bool Remove<Type>(string id) where Type : Transaction
        {
            try
            {
                return _transactions.OfType<Type>().DeleteOne(x => x.Id == id).DeletedCount > 0;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}