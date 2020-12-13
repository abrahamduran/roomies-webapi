using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Roomies.Tests.Mocks;
using Roomies.WebAPI.Controllers;
using Roomies.App.Models;
using Xunit;

namespace Roomies.Tests.UnitTests
{
    public class TransactionsControllerTests
    {
        private readonly TransactionsRepositoryMock _transactions;

        public TransactionsControllerTests()
        {
            _transactions = new TransactionsRepositoryMock();
        }

        [Fact]
        public void Get_TransactionLists_ReturnsListOfTransactions()
        {
            // arrange
            var controller = new TransactionsController(_transactions);
            var expected = new List<Transaction>() { Mock.Models.SimpleExpense(), Mock.Models.DetailedExpense(), Mock.Models.Payment(), Mock.Models.Payment() };
            _transactions.Transactions = expected;

            // act
            var result = controller.Get().Result;

            // assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsType<List<Transaction>>(ok.Value);
            Assert.Equal(expected, list);
        }
    }
}
