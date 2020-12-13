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
    public class PaymentsControllerTests
    {
        private readonly ExpensesRepositoryMock _expenses;
        private readonly PaymentsRepositoryMock _payments;
        private readonly RoommatesRepositoryMock _roommates;


        public PaymentsControllerTests()
        {
            _expenses = new ExpensesRepositoryMock();
            _payments = new PaymentsRepositoryMock();
            _roommates = new RoommatesRepositoryMock();
        }

        [Fact]
        public void Get_RequestPaymentsLists_ReturnsPaymentsWith200Status()
        {
            // arrange
            var controller = new PaymentsController(_payments, _expenses, _roommates);
            var payments = new List<Payment>() { Mock.Models.Payment(), Mock.Models.Payment() };
            var expected = payments.Select(x => PaymentResult.ForPayment(x, false)).ToList();
            _payments.Payments = payments;

            // act
            var result = controller.Get().Result;

            // assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsType<List<PaymentResult>>(ok.Value);
            Assert.Equal(expected, list);
        }

        [Fact]
        public void Get_RequestPaymentsLists_ReturnsEmptyListWith200Status()
        {
            // arrange
            var controller = new PaymentsController(_payments, _expenses, _roommates);
            var expected = new List<PaymentResult>();
            _payments.Payments = new List<Payment>(); ;

            // act
            var result = controller.Get().Result;

            // assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsType<List<PaymentResult>>(ok.Value);
            Assert.Equal(expected, list);
        }

        [Fact]
        public void Get_RequestPayment_ReturnsPaymentWith200Status()
        {
            // arrange
            var controller = new PaymentsController(_payments, _expenses, _roommates);
            var payment = Mock.Models.Payment();
            var expected = payment.Id;
            _payments.Payment = payment;

            // act
            var result = controller.Get(payment.Id).Result;

            // assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var actual = Assert.IsAssignableFrom<PaymentResult>(ok.Value);
            Assert.Equal(expected, actual.Id);
            Assert.NotNull(actual.Expenses);
        }

        [Fact]
        public void Get_RequestPayment_ReturnsNoPaymentWith404Status()
        {
            // arrange
            var controller = new PaymentsController(_payments, _expenses, _roommates);

            // act
            var result = controller.Get("").Result;

            // assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void Post_RegisterPaymentWithInvalidPaidAndExpenses_ProducesBadRequestResult()
        {
            // arrange
            var controller = new PaymentsController(_payments, _expenses, _roommates);
            var payment = Mock.Requests.Payment();
            _roommates.Roommates = new List<Roommate>();
            _expenses.Expenses = new List<Expense>();

            // act
            var result = controller.Post(payment).Result;

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.Equal(3, errors.Count);
            Assert.True(errors.ContainsKey("PaidTo"));
            Assert.True(errors.ContainsKey("PaidBy"));
            Assert.True(errors.ContainsKey("ExpenseIds"));
        }

        [Fact]
        public void Post_RegisterPaymentWithInvalidPaidBy_ProducesBadRequestResult()
        {
            // arrange
            var controller = new PaymentsController(_payments, _expenses, _roommates);
            var payment = Mock.Requests.Payment();
            _roommates.Roommates = new List<Roommate> { Mock.Models.Roommate(id: payment.PaidBy) };
            _expenses.Expenses = payment.ExpenseIds.Select(x => Mock.Models.SimpleExpense(id: x)).ToList();

            // act
            var result = controller.Post(payment).Result;

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.Single(errors);
            Assert.True(errors.ContainsKey("PaidTo"));
        }

        [Fact]
        public void Post_RegisterPaymentWithInvalidPaidTo_ProducesBadRequestResult()
        {
            // arrange
            var controller = new PaymentsController(_payments, _expenses, _roommates);
            var payment = Mock.Requests.Payment();
            _roommates.Roommates = new List<Roommate> { Mock.Models.Roommate(id: payment.PaidTo) };
            _expenses.Expenses = payment.ExpenseIds.Select(x => Mock.Models.SimpleExpense(id: x)).ToList();

            // act
            var result = controller.Post(payment).Result;

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.Single(errors);
            Assert.True(errors.ContainsKey("PaidBy"));
        }

        [Fact]
        public void Post_RegisterPaymentWithInvalidExpensesIds_ProducesBadRequestResult()
        {
            // arrange
            var controller = new PaymentsController(_payments, _expenses, _roommates);
            var payment = Mock.Requests.Payment();
            _roommates.Roommates = new List<Roommate> { Mock.Models.Roommate(id: payment.PaidBy), Mock.Models.Roommate(id: payment.PaidTo) };
            _expenses.Expenses = new List<Expense>();

            // act
            var result = controller.Post(payment).Result;

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.Single(errors);
            Assert.True(errors.ContainsKey("ExpenseIds"));
        }

        [Fact]
        public void Post_RegisterPaymentWithEmptyExpenses_ProducesBadRequestResult()
        {
            // arrange
            var controller = new PaymentsController(_payments, _expenses, _roommates);
            var payment = Mock.Requests.Payment(expenseIds: new string[0]);
            _roommates.Roommates = new List<Roommate> { Mock.Models.Roommate(id: payment.PaidBy), Mock.Models.Roommate(id: payment.PaidTo) };
            _expenses.Expenses = new List<Expense>();

            // act
            var result = controller.Post(payment).Result;

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.Single(errors);
            Assert.True(errors.ContainsKey("ExpenseIds"));
        }

        [Fact]
        public void Post_RegisterPaymentWithUnrelatedExpensesAndPaid_ProducesBadRequestResult()
        {
            // arrange
            var controller = new PaymentsController(_payments, _expenses, _roommates);
            var payment = Mock.Requests.Payment();
            _roommates.Roommates = new List<Roommate> { Mock.Models.Roommate(id: payment.PaidBy), Mock.Models.Roommate(id: payment.PaidTo) };
            _expenses.Expenses = payment.ExpenseIds.Select(x => Mock.Models.SimpleExpense(id: x)).ToList();

            // act
            var result = controller.Post(payment).Result;

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.Equal(3, errors.Count);
            Assert.True(errors.ContainsKey("PaidTo"));
            Assert.True(errors.ContainsKey("PaidBy"));
            Assert.True(errors.ContainsKey("ExpenseIds"));
        }

        [Fact]
        public void Post_RegisterPaymentWithUnrelatedExpensesAndPaidBy_ProducesBadRequestResult()
        {
            // arrange
            var controller = new PaymentsController(_payments, _expenses, _roommates);
            var payment = Mock.Requests.Payment();
            _roommates.Roommates = new List<Roommate> { Mock.Models.Roommate(id: payment.PaidBy), Mock.Models.Roommate(id: payment.PaidTo) };
            _expenses.Expenses = payment.ExpenseIds.Select(x => Mock.Models.SimpleExpense(id: x, payee: Mock.Models.Payee(id: payment.PaidTo))).ToList();

            // act
            var result = controller.Post(payment).Result;

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.Equal(2, errors.Count);
            Assert.True(errors.ContainsKey("PaidBy"));
            Assert.True(errors.ContainsKey("ExpenseIds"));
        }

        [Fact]
        public void Post_RegisterPaymentWithUnrelatedExpensesAndPaidTo_ProducesBadRequestResult()
        {
            // arrange
            var controller = new PaymentsController(_payments, _expenses, _roommates);
            var payment = Mock.Requests.Payment();
            _roommates.Roommates = new List<Roommate> { Mock.Models.Roommate(id: payment.PaidBy), Mock.Models.Roommate(id: payment.PaidTo) };
            _expenses.Expenses = payment.ExpenseIds.Select(x => Mock.Models.SimpleExpense(id: x, payers: new[] { Mock.Models.Payer(id: payment.PaidBy) })).ToList();

            // act
            var result = controller.Post(payment).Result;

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.Equal(2, errors.Count);
            Assert.True(errors.ContainsKey("PaidTo"));
            Assert.True(errors.ContainsKey("ExpenseIds"));
        }

        [Fact]
        public void Post_RegisterPartialPayment_ProducesBadRequestResult()
        {
            // arrange
            var controller = new PaymentsController(_payments, _expenses, _roommates);
            var payment = Mock.Requests.Payment();
            _roommates.Roommates = new List<Roommate> { Mock.Models.Roommate(id: payment.PaidBy), Mock.Models.Roommate(id: payment.PaidTo) };
            _expenses.Expenses = payment.ExpenseIds.Select(x => Mock.Models.SimpleExpense(id: x, payee: Mock.Models.Payee(id: payment.PaidTo), payers: new[] { Mock.Models.Payer(id: payment.PaidBy) })).ToList();

            // act
            var result = controller.Post(payment).Result;

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.Single(errors);
            Assert.True(errors.ContainsKey("Amount"));
        }

        [Fact]
        public void Post_RegisteredPaymentDate_IsEqualsToCurrentDate()
        {
            // arrange
            var controller = new PaymentsController(_payments, _expenses, _roommates);
            var expected = DateTime.Now.ToShortDateString();
            var payment = Mock.Requests.Payment(amount: 2);
            _roommates.Roommates = new List<Roommate> { Mock.Models.Roommate(id: payment.PaidBy), Mock.Models.Roommate(id: payment.PaidTo) };
            _expenses.Expenses = payment.ExpenseIds.Select(x => Mock.Models.SimpleExpense(id: x, payee: Mock.Models.Payee(id: payment.PaidTo), payers: new[] { Mock.Models.Payer(id: payment.PaidBy) })).ToList();

            // act
            var result = controller.Post(payment).Result;

            // assert
            var created = Assert.IsType<CreatedAtActionResult>(result);
            var actual = Assert.IsAssignableFrom<PaymentResult>(created.Value);
            var date = DateTime.UnixEpoch.AddSeconds(actual.Date).ToShortDateString();
            Assert.Equal(expected, date);
        }

        [Fact]
        public void Post_RegisterPayment_ReturnPaymentResultWithNullExpenses()
        {
            // arrange
            var controller = new PaymentsController(_payments, _expenses, _roommates);
            var payment = Mock.Requests.Payment(amount: 2);
            _roommates.Roommates = new List<Roommate> { Mock.Models.Roommate(id: payment.PaidBy), Mock.Models.Roommate(id: payment.PaidTo) };
            _expenses.Expenses = payment.ExpenseIds.Select(x => Mock.Models.SimpleExpense(id: x, payee: Mock.Models.Payee(id: payment.PaidTo), payers: new[] { Mock.Models.Payer(id: payment.PaidBy) })).ToList();

            // act
            var result = controller.Post(payment).Result;

            // assert
            var created = Assert.IsType<CreatedAtActionResult>(result);
            var actual = Assert.IsAssignableFrom<PaymentResult>(created.Value);
            Assert.Null(actual.Expenses);
        }

        [Fact]
        public void Post_RegisteredPaymentExpenses_ContainsAllTheSelectedExpenses()
        {
            // arrange
            var controller = new PaymentsController(_payments, _expenses, _roommates);
            var payment = Mock.Requests.Payment(amount: 2);
            var expected = payment.ExpenseIds;
            _roommates.Roommates = new List<Roommate> { Mock.Models.Roommate(id: payment.PaidBy), Mock.Models.Roommate(id: payment.PaidTo) };
            _expenses.Expenses = payment.ExpenseIds.Select(x => Mock.Models.SimpleExpense(id: x, payee: Mock.Models.Payee(id: payment.PaidTo), payers: new[] { Mock.Models.Payer(id: payment.PaidBy) })).ToList();

            // act
            var result = controller.Post(payment).Result;

            // assert
            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.IsAssignableFrom<PaymentResult>(created.Value);
            Assert.Equal(expected, _payments.Payment.Expenses.Select(x => x.Id).ToArray());
        }

        [Theory]
        [InlineData(100, 0, -100)]
        [InlineData(100, 20, -80)]
        [InlineData(200, 500, 300)]
        [InlineData(200, -500, -700)]
        [InlineData(200, -100, -300)]
        public void Post_RegisterPayment_UpdatesPayerBalance(decimal total, decimal balance, decimal expected)
        {
            // arrange
            var controller = new PaymentsController(_payments, _expenses, _roommates);
            var payment = Mock.Requests.Payment(amount: total, expenseIds: new[] { "1" });
            var payer = Mock.Models.Roommate(id: payment.PaidBy, balance: balance);
            var payee = Mock.Models.Roommate(id: payment.PaidTo);
            _roommates.Roommates = new List<Roommate> { payer, payee };
            _expenses.Expenses = payment.ExpenseIds.Select(x =>
                Mock.Models.SimpleExpense(
                    id: x,
                    payee: Mock.Models.Payee(id: payment.PaidTo),
                    payers: new[] { Mock.Models.Payer(id: payment.PaidBy, amount: total) },
                    total: total
                )
            ).ToList();

            // act
            var result = controller.Post(payment).Result;

            // assert
            var created = Assert.IsType<CreatedAtActionResult>(result);
            var actual = Assert.IsAssignableFrom<PaymentResult>(created.Value);
            Assert.Equal(expected, payer.Balance);
        }

        [Theory]
        [InlineData(100, 0, 100)]
        [InlineData(100, 20, 120)]
        [InlineData(200, 500, 700)]
        [InlineData(200, -200, 0)]
        [InlineData(200, -100, 100)]
        [InlineData(200, -500, -300)]
        public void Post_RegisterPayment_UpdatesPayeeBalance(decimal total, decimal balance, decimal expected)
        {
            // arrange
            var controller = new PaymentsController(_payments, _expenses, _roommates);
            var payment = Mock.Requests.Payment(amount: total, expenseIds: new[] { "1" });
            var payer = Mock.Models.Roommate(id: payment.PaidBy);
            var payee = Mock.Models.Roommate(id: payment.PaidTo, balance: balance);
            _roommates.Roommates = new List<Roommate> { payer, payee };
            _expenses.Expenses = payment.ExpenseIds.Select(x =>
                Mock.Models.SimpleExpense(
                    id: x,
                    payee: Mock.Models.Payee(id: payment.PaidTo),
                    payers: new[] { Mock.Models.Payer(id: payment.PaidBy, amount: total) },
                    total: total
                )
            ).ToList();

            // act
            var result = controller.Post(payment).Result;

            // assert
            var created = Assert.IsType<CreatedAtActionResult>(result);
            var actual = Assert.IsAssignableFrom<PaymentResult>(created.Value);
            Assert.Equal(expected, payee.Balance);
        }

        [Theory, MemberData(nameof(MultiplePayersBalance))]
        public void Post_RegisterPayment_UpdatesMultiplePayersBalance(decimal paymentTotal, decimal expenseTotal, decimal[] amounts, decimal[] balances, decimal[] expected)
        {
            // arrange
            var controller = new PaymentsController(_payments, _expenses, _roommates);
            var payment = Mock.Requests.Payment(amount: paymentTotal, expenseIds: new[] { "1" });
            var roommatePayers = balances.Select(x => Mock.Models.Roommate(balance: x)).ToArray();
            var expensePayers = amounts.Select(x => Mock.Models.Payer(amount: x)).ToArray();
            roommatePayers[0].Id = payment.PaidBy;
            expensePayers[0].Id = payment.PaidBy;
            var payee = Mock.Models.Roommate(id: payment.PaidTo);
            var roommates = new List<Roommate> { payee };
            roommates.AddRange(roommatePayers);
            _roommates.Roommates = roommates;
            _expenses.Expenses = payment.ExpenseIds.Select(x =>
                Mock.Models.SimpleExpense(
                    id: x,
                    payee: Mock.Models.Payee(id: payment.PaidTo),
                    payers: expensePayers,
                    total: expenseTotal
                )
            ).ToList();

            // act
            var result = controller.Post(payment).Result;

            // assert
            var created = Assert.IsType<CreatedAtActionResult>(result);
            var actual = Assert.IsAssignableFrom<PaymentResult>(created.Value);
            Assert.Equal(expected, roommatePayers.Select(x => x.Balance).ToArray());
        }

        [Fact]
        public void Post_RegisterPayment_DoesNotAlterExpenseStatusUnlessFullyPaid()
        {
            // arrange
            var controller = new PaymentsController(_payments, _expenses, _roommates);
            var payment = Mock.Requests.Payment(amount: 200, expenseIds: new[] { "1" });
            var expense = Mock.Models.SimpleExpense(
                    id: "1",
                    payee: Mock.Models.Payee(id: payment.PaidTo),
                    payers: new[] { Mock.Models.Payer(id: payment.PaidBy, amount: 200), Mock.Models.Payer(amount: 200) },
                    total: 400
                );
            var payer = Mock.Models.Roommate(id: payment.PaidBy);
            var payee = Mock.Models.Roommate(id: payment.PaidTo);
            _roommates.Roommates = new List<Roommate> { payer, payee };
            _expenses.Expenses = new List<Expense> { expense };

            // act
            var result = controller.Post(payment).Result;

            // assert
            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.IsAssignableFrom<PaymentResult>(created.Value);
            Assert.Equal(ExpenseStatus.Unpaid, expense.Status);
        }

        [Fact]
        public void Post_RegisterPayment_AlterExpenseStatusWhenFullyPaid()
        {
            // arrange
            var controller = new PaymentsController(_payments, _expenses, _roommates);
            var payment = Mock.Requests.Payment(amount: 200, expenseIds: new[] { "1" });
            var expense = Mock.Models.SimpleExpense(
                    id: "1",
                    payee: Mock.Models.Payee(id: payment.PaidTo),
                    payers: new[] { Mock.Models.Payer(id: payment.PaidBy, amount: 200) },
                    total: 200
                );
            var payer = Mock.Models.Roommate(id: payment.PaidBy);
            var payee = Mock.Models.Roommate(id: payment.PaidTo);
            _roommates.Roommates = new List<Roommate> { payer, payee };
            _expenses.Expenses = new List<Expense> { expense };

            // act
            var result = controller.Post(payment).Result;

            // assert
            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.IsAssignableFrom<PaymentResult>(created.Value);
            Assert.Equal(ExpenseStatus.Paid, expense.Status);
        }

        [Fact]
        // Basically, if I paid for an expense, but I am part of the payers, the expense
        // should be marked Paid when the rest of the payers have paid, excluding me.
        // Because, I won't pay myself ಠ_ಠ
        public void Post_RegisterPaymentForExpenseWithSelfPayee_AlterExpenseStatusWhenFullyPaid()
        {
            // arrange
            var controller = new PaymentsController(_payments, _expenses, _roommates);
            var payment = Mock.Requests.Payment(amount: 200, expenseIds: new[] { "1" });
            var expense = Mock.Models.SimpleExpense(
                    id: "1",
                    payee: Mock.Models.Payee(id: payment.PaidTo),
                    payers: new[] { Mock.Models.Payer(id: payment.PaidBy, amount: 200), Mock.Models.Payer(id: payment.PaidTo, amount: 200) },
                    total: 400
                );
            var payer = Mock.Models.Roommate(id: payment.PaidBy);
            var payee = Mock.Models.Roommate(id: payment.PaidTo);
            _roommates.Roommates = new List<Roommate> { payer, payee };
            _expenses.Expenses = new List<Expense> { expense };

            // act
            var result = controller.Post(payment).Result;

            // assert
            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.IsAssignableFrom<PaymentResult>(created.Value);
            Assert.Equal(ExpenseStatus.Paid, expense.Status);
        }

        [Fact]
        public void Post_RegisterPayment_ProducesCorrectSummary()
        {
            // arrange
            var controller = new PaymentsController(_payments, _expenses, _roommates);
            var payment = Mock.Requests.Payment(amount: 200, expenseIds: new[] { "1" });
            var expense = Mock.Models.SimpleExpense(
                    id: "1",
                    payee: Mock.Models.Payee(id: payment.PaidTo),
                    payers: new[] { Mock.Models.Payer(id: payment.PaidBy, amount: 200) },
                    total: 200
                );
            var payer = Mock.Models.Roommate(id: payment.PaidBy);
            var payee = Mock.Models.Roommate(id: payment.PaidTo);
            _roommates.Roommates = new List<Roommate> { payer, payee };
            _expenses.Expenses = new List<Expense> { expense };

            // act
            var result = controller.Post(payment).Result;

            // assert
            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.IsAssignableFrom<PaymentResult>(created.Value);
            Assert.Equal(payment.Amount, _expenses.PaymentUpdates[0].Summary.Amount);
            Assert.Equal(payment.PaidBy, _expenses.PaymentUpdates[0].Summary.By.Id);
        }

        [Fact]
        public void Post_RegisterPayment_AllowsAmountOffset()
        {
            // arrange
            var controller = new PaymentsController(_payments, _expenses, _roommates);
            var expected = 2.08M;
            var payment = Mock.Requests.Payment(amount: 2.08M);
            _roommates.Roommates = new List<Roommate> { Mock.Models.Roommate(id: payment.PaidBy), Mock.Models.Roommate(id: payment.PaidTo) };
            _expenses.Expenses = payment.ExpenseIds
                .Select(x => Mock.Models.SimpleExpense(id: x, total: 2.073M, payee: Mock.Models.Payee(id: payment.PaidTo), payers: new[] { Mock.Models.Payer(id: payment.PaidBy) }))
                .ToList();

            // act
            var result = controller.Post(payment).Result;

            // assert
            var created = Assert.IsType<CreatedAtActionResult>(result);
            var actual = Assert.IsAssignableFrom<PaymentResult>(created.Value);
            Assert.Equal(expected, actual.Total);
        }

        public static IEnumerable<object[]> MultiplePayersBalance
        {
            get
            {
                return new[]
                {
                    new object[] {
                        200M, 1200M,
                        new[] { 200M, 1000M, 1000M },
                        new[] { -200M, 40M, 40M },
                        new[] { -400M, 40M, 40M }
                    },
                    new object[] {
                        200M, 1200M,
                        new[] { 200M, 1000M, 1000M },
                        new[] { 200M, 0M, 0M },
                        new[] { 0M, 0M, 0M }
                    },
                    new object[] {
                        200M, 1200M,
                        new[] { 200M, 1000M, 1000M },
                        new[] { 0M, 0M, 0M },
                        new[] { -200M, 0M, 0M }
                    }
                };
            }
        }

    }
}
