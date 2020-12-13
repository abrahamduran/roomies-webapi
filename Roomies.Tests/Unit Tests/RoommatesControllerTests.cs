using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Roomies.Tests.Mocks;
using Roomies.WebAPI.Controllers;
using Roomies.App.Models;
using Roomies.WebAPI.Responses;
using Xunit;

namespace Roomies.Tests.UnitTests
{
    public class RoommatesControllerTests
    {
        private readonly RoommatesRepositoryMock _roommates;
        private readonly ExpensesRepositoryMock _expenses;
        private readonly PaymentsRepositoryMock _payments;

        public RoommatesControllerTests()
        {
            _roommates = new RoommatesRepositoryMock();
            _expenses = new ExpensesRepositoryMock();
            _payments = new PaymentsRepositoryMock();
        }

        [Fact]
        public void Get_RequestRoommatesLists_ReturnsRoommatesWith200Status()
        {
            // arrange
            var controller = new RoommatesController(_roommates, _expenses, _payments);
            var expected = new List<Roommate>() { Mock.Models.Roommate(), Mock.Models.Roommate() };
            _roommates.Roommates = expected;

            // act
            var result = controller.Get().Result;

            // assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsType<List<Roommate>>(ok.Value);
            Assert.Equal(expected, list);
        }

        [Fact]
        public void Get_RequestExpensesLists_ReturnsEmptyListWith200Status()
        {
            // arrange
            var controller = new RoommatesController(_roommates, _expenses, _payments);
            var expected = new List<Roommate>();
            _roommates.Roommates = expected;

            // act
            var result = controller.Get().Result;

            // assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsType<List<Roommate>>(ok.Value);
            Assert.Equal(expected, list);
        }

        [Fact]
        public void Get_RequestExpense_ReturnsSingleExpenseWith200Status()
        {
            // arrange
            var controller = new RoommatesController(_roommates, _expenses, _payments);
            var roommate = Mock.Models.Roommate();
            var expected = roommate.Id;
            _roommates.Roommate = roommate;

            // act
            var result = controller.Get(roommate.Id).Result;

            // assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var value = Assert.IsAssignableFrom<Roommate>(ok.Value);
            Assert.Equal(expected, value.Id);
        }

        [Fact]
        public void Get_RequestExpense_ReturnsNoExpenseWith404Status()
        {
            // arrange
            var controller = new RoommatesController(_roommates, _expenses, _payments);

            // act
            var result = controller.Get("").Result;

            // assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void Post_CreateRoommate_ReturnsCreatedRoommateAndStoresIt()
        {
            // arrange
            var controller = new RoommatesController(_roommates, _expenses, _payments);
            var roommate = Mock.Requests.Roommate();
            var expected = (roommate.Name, roommate.Email);

            // act
            var result = controller.Post(roommate).Result;

            // assert
            var value = Assert.IsType<CreatedAtActionResult>(result);
            var actual = Assert.IsAssignableFrom<Roommate>(value.Value);
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.Email, actual.Email);
            Assert.Equal(expected.Name, _roommates.Roommate.Name);
            Assert.Equal(expected.Email, _roommates.Roommate.Email);
        }

        [Fact]
        public void Post_CreatedRoommate_HasInitialZeroBalance()
        {
            // arrange
            var controller = new RoommatesController(_roommates, _expenses, _payments);
            var expected = 0M;

            // act
            var result = controller.Post(Mock.Requests.Roommate()).Result;

            // assert
            var value = Assert.IsType<CreatedAtActionResult>(result);
            var roommate = Assert.IsAssignableFrom<Roommate>(value.Value);
            Assert.Equal(expected, _roommates.Roommate.Balance);
            Assert.Equal(expected, roommate.Balance);
        }

        [Fact]
        public void Get_RoommateExpenses_Returns404WhenRoommateIdIsNotPresentInDB()
        {
            // arrange
            var controller = new RoommatesController(_roommates, _expenses, _payments);

            // act
            var result = controller.GetExpenses("").Result;

            // assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void Get_RoommateExpenses_Returns404WhenRoommateHasNoExpenses()
        {
            // arrange
            var controller = new RoommatesController(_roommates, _expenses, _payments);
            _roommates.Roommate = Mock.Models.Roommate();

            // act
            var result = controller.GetExpenses(_roommates.Roommate.Id).Result;

            // assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void Get_RoommateExpenses_ReturnsRoommateExpensesResult()
        {
            // arrange
            var controller = new RoommatesController(_roommates, _expenses, _payments);
            _roommates.Roommate = Mock.Models.Roommate();
            _expenses.Expenses = new[] { Mock.Models.SimpleExpense(total: 120) };

            // act
            var result = controller.GetExpenses(_roommates.Roommate.Id).Result;

            // assert
            var value = Assert.IsType<OkObjectResult>(result);
            Assert.IsAssignableFrom<RoommateExpenses>(value.Value);
        }

        [Theory, MemberData(nameof(ExpensesWithTotal))]
        public void Get_RoommateExpenses_ReturnsRightTotalValue(decimal[] expenseTotals, decimal expected)
        {
            // arrange
            var controller = new RoommatesController(_roommates, _expenses, _payments);
            _roommates.Roommate = Mock.Models.Roommate();
            _expenses.Expenses = expenseTotals.Select(x =>
                Mock.Models.SimpleExpense(total: x, payers: new[] { Mock.Models.Payer(id: _roommates.Roommate.Id, amount: x) })
            );

            // act
            var result = controller.GetExpenses(_roommates.Roommate.Id).Result;

            // assert
            var value = Assert.IsType<OkObjectResult>(result);
            var actual = Assert.IsAssignableFrom<RoommateExpenses>(value.Value);
            Assert.Equal(expected, actual.YourTotal);
            Assert.Equal(expenseTotals.Count(), actual.Expenses.Count());
        }

        [Fact]
        public void Get_RoommateExpenses_HasNullPayers()
        {
            // arrange
            var controller = new RoommatesController(_roommates, _expenses, _payments);
            _roommates.Roommate = Mock.Models.Roommate();
            _expenses.Expenses = new[] { Mock.Models.SimpleExpense(payers: new[] { Mock.Models.Payer(id: _roommates.Roommate.Id) }) };

            // act
            var result = controller.GetExpenses(_roommates.Roommate.Id).Result;

            // assert
            var value = Assert.IsType<OkObjectResult>(result);
            var actual = Assert.IsAssignableFrom<RoommateExpenses>(value.Value);
            foreach (var item in actual.Expenses)
                Assert.Null(item.Payers);
        }

        [Fact]
        public void Get_RoommateExpenses_HasNullItems()
        {
            // arrange
            var controller = new RoommatesController(_roommates, _expenses, _payments);
            _roommates.Roommate = Mock.Models.Roommate();
            var expenseItem = Mock.Models.ExpenseItem(payers: new[] { Mock.Models.Payer(id: _roommates.Roommate.Id) });
            _expenses.Expenses = new[] { Mock.Models.DetailedExpense(items: new[] { expenseItem }) };

            // act
            var result = controller.GetExpenses("").Result;

            // assert
            var value = Assert.IsType<OkObjectResult>(result);
            var actual = Assert.IsAssignableFrom<RoommateExpenses>(value.Value);
            foreach (var item in actual.Expenses)
                Assert.Null(item.Items);
        }

        [Fact]
        public void Get_RoommateExpense_Returns404WhenRoommateIdIsNotPresentInDB()
        {
            // arrange
            var controller = new RoommatesController(_roommates, _expenses, _payments);

            // act
            var result = controller.GetExpense("", "").Result;

            // assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void Get_RoommateExpense_Returns404WhenExpenseIdIsNotPresentInDB()
        {
            // arrange
            var controller = new RoommatesController(_roommates, _expenses, _payments);
            _roommates.Roommate = Mock.Models.Roommate();

            // act
            var result = controller.GetExpense(_roommates.Roommate.Id, "").Result;

            // assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void Get_RoommateExpense_Returns404WhenRoommateHasNoExpenses()
        {
            // arrange
            var controller = new RoommatesController(_roommates, _expenses, _payments);
            _roommates.Roommate = Mock.Models.Roommate();

            // act
            var result = controller.GetExpense(_roommates.Roommate.Id, "").Result;

            // assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void Get_RoommateExpense_ReturnsRoommateExpenseResult()
        {
            // arrange
            var controller = new RoommatesController(_roommates, _expenses, _payments);
            _roommates.Roommate = Mock.Models.Roommate();
            _expenses.Expense = Mock.Models.SimpleExpense(total: 120, payers: new[] { Mock.Models.Payer(id: _roommates.Roommate.Id, amount: 120) });

            // act
            var result = controller.GetExpense(_roommates.Roommate.Id, _expenses.Expense.Id).Result;

            // assert
            var value = Assert.IsType<OkObjectResult>(result);
            Assert.IsAssignableFrom<RoommateExpense>(value.Value);
        }

        [Theory, MemberData(nameof(ExpenseWithTotal))]
        public void Get_RoommateExpense_ReturnsRightTotalValue(decimal expenseTotal, decimal expected)
        {
            // arrange
            var controller = new RoommatesController(_roommates, _expenses, _payments);
            _roommates.Roommate = Mock.Models.Roommate();
            _expenses.Expense = Mock.Models.SimpleExpense(total: expenseTotal, payers: new[] { Mock.Models.Payer(id: _roommates.Roommate.Id, amount: expenseTotal) });

            // act
            var result = controller.GetExpense(_roommates.Roommate.Id, _expenses.Expense.Id).Result;

            // assert
            var value = Assert.IsType<OkObjectResult>(result);
            var actual = Assert.IsAssignableFrom<RoommateExpense>(value.Value);
            Assert.Equal(expected, actual.Expense.Total);
        }

        [Fact]
        public void Get_RoommateExpense_HasNullPayers()
        {
            // arrange
            var controller = new RoommatesController(_roommates, _expenses, _payments);
            _roommates.Roommate = Mock.Models.Roommate();
            _expenses.Expense = Mock.Models.SimpleExpense(payers: new[] { Mock.Models.Payer(id: _roommates.Roommate.Id) });

            // act
            var result = controller.GetExpense(_roommates.Roommate.Id, _expenses.Expense.Id).Result;

            // assert
            var value = Assert.IsType<OkObjectResult>(result);
            var actual = Assert.IsAssignableFrom<RoommateExpense>(value.Value);
            Assert.Null(actual.Expense.Payers);
        }

        [Fact]
        public void Get_RoommateExpense_HasNullItems()
        {
            // arrange
            var controller = new RoommatesController(_roommates, _expenses, _payments);
            _roommates.Roommate = Mock.Models.Roommate();
            var expenseItem = Mock.Models.ExpenseItem(payers: new[] { Mock.Models.Payer(id: _roommates.Roommate.Id) });
            _expenses.Expense = Mock.Models.DetailedExpense(items: new[] { expenseItem });

            // act
            var result = controller.GetExpense(_roommates.Roommate.Id, _expenses.Expense.Id).Result;

            // assert
            var value = Assert.IsType<OkObjectResult>(result);
            var actual = Assert.IsAssignableFrom<RoommateExpense>(value.Value);
            Assert.Null(actual.Expense.Items);
        }

        [Fact]
        public void Get_RoommatePayments_Returns404WhenRoommateIdIsNotPresentInDB()
        {
            // arrange
            var controller = new RoommatesController(_roommates, _expenses, _payments);

            // act
            var result = controller.GetPayments("").Result;

            // assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void Get_RoommatePayments_Returns404WhenRoommateHasNoPayments()
        {
            // arrange
            var controller = new RoommatesController(_roommates, _expenses, _payments);
            _roommates.Roommate = Mock.Models.Roommate();

            // act
            var result = controller.GetPayments(_roommates.Roommate.Id).Result;

            // assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void Get_RoommatePayments_ReturnsRoommatePaymentsResult()
        {
            // arrange
            var controller = new RoommatesController(_roommates, _expenses, _payments);
            _roommates.Roommate = Mock.Models.Roommate();
            _payments.Payments = new[] { Mock.Models.Payment(by: Mock.Models.Payee(id: _roommates.Roommate.Id)) };

            // act
            var result = controller.GetPayments(_roommates.Roommate.Id).Result;

            // assert
            var value = Assert.IsType<OkObjectResult>(result);
            Assert.IsAssignableFrom<RoommatePayments>(value.Value);
        }

        [Theory, MemberData(nameof(PaymentsWithTotal))]
        public void Get_RoommatePayments_ReturnsRightTotalValue(decimal[] paymentTotals, decimal expected)
        {
            // arrange
            var controller = new RoommatesController(_roommates, _expenses, _payments);
            _roommates.Roommate = Mock.Models.Roommate();
            _payments.Payments = paymentTotals.Select(x =>
                Mock.Models.Payment(total: x, by: Mock.Models.Payee(id: _roommates.Roommate.Id))
            );

            // act
            var result = controller.GetPayments(_roommates.Roommate.Id).Result;

            // assert
            var value = Assert.IsType<OkObjectResult>(result);
            var actual = Assert.IsAssignableFrom<RoommatePayments>(value.Value);
            Assert.Equal(expected, actual.YourTotal);
            Assert.Equal(paymentTotals.Count(), actual.Payments.Count());
        }

        [Fact]
        public void Get_RoommatePayments_HasByPropertyNull()
        {
            // arrange
            var controller = new RoommatesController(_roommates, _expenses, _payments);
            _roommates.Roommate = Mock.Models.Roommate();
            _payments.Payments = new[] { Mock.Models.Payment(by: Mock.Models.Payee(id: _roommates.Roommate.Id)) };

            // act
            var result = controller.GetPayments(_roommates.Roommate.Id).Result;

            // assert
            var value = Assert.IsType<OkObjectResult>(result);
            var actual = Assert.IsAssignableFrom<RoommatePayments>(value.Value);
            foreach (var item in actual.Payments)
                Assert.Null(item.By);
        }

        // FOOOOOO


        [Fact]
        public void Get_RoommatePayment_Returns404WhenRoommateIdIsNotPresentInDB()
        {
            // arrange
            var controller = new RoommatesController(_roommates, _expenses, _payments);

            // act
            var result = controller.GetPayment("", "").Result;

            // assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void Get_RoommatePayment_Returns404WhenPaymentIdIsNotPresentInDB()
        {
            // arrange
            var controller = new RoommatesController(_roommates, _expenses, _payments);
            _roommates.Roommate = Mock.Models.Roommate();

            // act
            var result = controller.GetPayment(_roommates.Roommate.Id, "").Result;

            // assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void Get_RoommatePayment_ReturnsRoommatePaymentResult()
        {
            // arrange
            var controller = new RoommatesController(_roommates, _expenses, _payments);
            _roommates.Roommate = Mock.Models.Roommate();
            _payments.Payment = Mock.Models.Payment(by: Mock.Models.Payee(id: _roommates.Roommate.Id));

            // act
            var result = controller.GetPayment(_roommates.Roommate.Id, _payments.Payment.Id).Result;

            // assert
            var value = Assert.IsType<OkObjectResult>(result);
            Assert.IsAssignableFrom<RoommatePayment>(value.Value);
        }

        [Theory, MemberData(nameof(PaymentWithTotal))]
        public void Get_RoommatePayment_ReturnsRightTotalValue(decimal paymentTotal, decimal expected)
        {
            // arrange
            var controller = new RoommatesController(_roommates, _expenses, _payments);
            _roommates.Roommate = Mock.Models.Roommate();
            _payments.Payment = Mock.Models.Payment(total: paymentTotal, by: Mock.Models.Payee(id: _roommates.Roommate.Id));

            // act
            var result = controller.GetPayment(_roommates.Roommate.Id, _payments.Payment.Id).Result;

            // assert
            var value = Assert.IsType<OkObjectResult>(result);
            var actual = Assert.IsAssignableFrom<RoommatePayment>(value.Value);
            Assert.Equal(expected, actual.YourTotal);
        }

        [Fact]
        public void Get_RoommatePayment_HasByPropertyNull()
        {
            // arrange
            var controller = new RoommatesController(_roommates, _expenses, _payments);
            _roommates.Roommate = Mock.Models.Roommate();
            _payments.Payment = Mock.Models.Payment(by: Mock.Models.Payee(id: _roommates.Roommate.Id));

            // act
            var result = controller.GetPayment(_roommates.Roommate.Id, _payments.Payment.Id).Result;

            // assert
            var value = Assert.IsType<OkObjectResult>(result);
            var actual = Assert.IsAssignableFrom<RoommatePayment>(value.Value);
            Assert.Null(actual.Payment.By);
        }

        public static IEnumerable<object[]> ExpensesWithTotal
        {
            get
            {
                return new[]
                {
                    new object[] {
                        new[] { 200M, 1000M, 1000M },
                        2200M
                    },
                    new object[] {
                        new[] { 200M, 100M, 130M },
                        430M
                    },
                    new object[] {
                        new[] { 20.46M, 10.78M, 1.13M },
                        32.37M
                    },
                    new object[] {
                        new[] { 20.46M },
                        20.46M
                    },
                    new object[] {
                        new[] { 2.6M, 2.6M },
                        5.2M
                    },
                    new object[] {
                        new[] { 2.6M, 1.2M, 3.3M, 4.1M, 9999M, 5.9M, 1M, 3M, 4M, 7M, 9M },
                        10040.1M
                    }
                };
            }
        }

        public static IEnumerable<object[]> ExpenseWithTotal
        {
            get
            {
                return new[]
                {
                    new object[] { 2200M, 2200M },
                    new object[] { 430M, 430M },
                    new object[] { 32.37M, 32.37M },
                    new object[] { 20.46M, 20.46M },
                    new object[] { 5.2M, 5.2M },
                    new object[] { 10040.1M, 10040.1M }
                };
            }
        }

        public static IEnumerable<object[]> PaymentsWithTotal
        {
            get
            {
                return new[]
                {
                    new object[] {
                        new[] { 200M, 1000M, 1000M },
                        2200M
                    },
                    new object[] {
                        new[] { 200M, 100M, 130M },
                        430M
                    },
                    new object[] {
                        new[] { 20.46M, 10.78M, 1.13M },
                        32.37M
                    },
                    new object[] {
                        new[] { 20.46M },
                        20.46M
                    },
                    new object[] {
                        new[] { 2.6M, 2.6M },
                        5.2M
                    },
                    new object[] {
                        new[] { 2.6M, 1.2M, 3.3M, 4.1M, 9999M, 5.9M, 1M, 3M, 4M, 7M, 9M },
                        10040.1M
                    }
                };
            }
        }

        public static IEnumerable<object[]> PaymentWithTotal
        {
            get
            {
                return new[]
                {
                    new object[] { 2200M, 2200M },
                    new object[] { 430M, 430M },
                    new object[] { 32.37M, 32.37M },
                    new object[] { 20.46M, 20.46M },
                    new object[] { 5.2M, 5.2M },
                    new object[] { 10040.1M, 10040.1M }
                };
            }
        }
    }
}
