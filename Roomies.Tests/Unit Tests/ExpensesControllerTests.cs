using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Roomies.Tests.Mocks;
using Roomies.WebAPI.Controllers;
using Roomies.WebAPI.Models;
using Roomies.WebAPI.Requests;
using Xunit;

namespace Roomies.Tests.UnitTests
{
    public class ExpensesControllerTests
    {
        private readonly ExpensesRepositoryMock _expenses;
        private readonly RoommatesRepositoryMock _roommates;
        private readonly ChannelMock<IEnumerable<Autocomplete>> _channel;

        public ExpensesControllerTests()
        {
            _expenses = new ExpensesRepositoryMock();
            _roommates = new RoommatesRepositoryMock();
            _channel = new ChannelMock<IEnumerable<Autocomplete>>(new ChannelWriterMock<IEnumerable<Autocomplete>>());
        }

        [Fact]
        public void Get_RequestExpensesLists_ReturnsExpensesWith200Status()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var expected = new List<Expense>() { Mock.Models.SimpleExpense(), Mock.Models.DetailedExpense() };
            _expenses.Expenses = expected;

            // act
            var result = controller.Get().Result;
            
            // assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsType<List<Expense>>(ok.Value);
            Assert.Equal(expected, list);
        }

        [Fact]
        public void Get_RequestExpensesLists_ReturnsEmptyListWith200Status()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var expected = new List<Expense>();
            _expenses.Expenses = expected;

            // act
            var result = controller.Get().Result;

            // assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsType<List<Expense>>(ok.Value);
            Assert.Equal(expected, list);
        }

        [Fact]
        public void Get_RequestExpense_ReturnsSingleExpenseWith200Status()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var expense = Mock.Models.SimpleExpense();
            var expected = expense.Id;
            _expenses.Expense = expense;

            // act
            var result = controller.Get(expense.Id).Result;

            // assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var value = Assert.IsAssignableFrom<Expense>(ok.Value);
            Assert.Equal(expected, value.Id);
        }

        [Fact]
        public void Get_RequestExpense_ReturnsNoExpenseWith404Status()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            
            // act
            var result = controller.Get("").Result;

            // assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Post_RegisterEmptyExpense_ProducesBadRequestResult()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var registerExpense = new RegisterExpense();

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        #region Simple Expense
        [Fact]
        public async Task Post_RegisterSimpleExpenseWithNullDistribution_ProducesBadRequestResult()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer() };
            var registerExpense = Mock.Requests.RegisterSimpleExpense(distribution: null, payers: payers);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Distribution"));
        }

        [Theory, MemberData(nameof(EvenDistributions))]
        public async Task Post_RegisterSimpleExpense_DistributesEvenExpense(decimal total, decimal[] expected)
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = expected.Select(x => Mock.Requests.Payer()).ToArray();
            var registerExpense = Mock.Requests.RegisterSimpleExpense(distribution: ExpenseDistribution.Even, total: total, payers: payers);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            var created = Assert.IsType<CreatedAtActionResult>(result);
            var expense = Assert.IsAssignableFrom<SimpleExpense>(created.Value);
            Assert.Equal(expected, expense.Payers.Select(x => x.Amount));
        }

        [Theory, MemberData(nameof(CustomDistributions))]
        public async Task Post_RegisterSimpleExpense_DistributesCustomExpense(decimal total, decimal[] expected)
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = expected.Select(x => Mock.Requests.Payer(amount: x)).ToArray();
            var registerExpense = Mock.Requests.RegisterSimpleExpense(distribution: ExpenseDistribution.Custom, total: total, payers: payers);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            var created = Assert.IsType<CreatedAtActionResult>(result);
            var expense = Assert.IsAssignableFrom<SimpleExpense>(created.Value);
            Assert.Equal(expected, expense.Payers.Select(x => x.Amount));
        }

        [Theory, MemberData(nameof(ProportionalDistributions))]
        public async Task Post_RegisterSimpleExpense_DistributesProportionalExpense(decimal total, double[] multipliers, decimal[] expected)
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = multipliers.Select(x => Mock.Requests.Payer(multiplier: x)).ToArray();
            var registerExpense = Mock.Requests.RegisterSimpleExpense(distribution: ExpenseDistribution.Proportional, total: total, payers: payers);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            var created = Assert.IsType<CreatedAtActionResult>(result);
            var expense = Assert.IsAssignableFrom<SimpleExpense>(created.Value);
            Assert.Equal(expected, expense.Payers.Select(x => x.Amount));
        }

        [Fact]
        public async Task Post_RegisterSimpleExpenseWithIncorrectTotal_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(amount: 120) };
            var total = 2000M;
            var registerExpense = Mock.Requests.RegisterSimpleExpense(distribution: ExpenseDistribution.Custom, total: total, payers: payers);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.Equal(4, errors.Values.SelectMany(x => (string[])x).Count());
            Assert.True(errors.ContainsKey("Total"));
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public async Task Post_RegisterSimpleExpense_UpdatesAutocompleteIndex()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var expected = new List<IEnumerable<Autocomplete>> {
                new List<Autocomplete>
                {
                    new Autocomplete { Text = "Business", Type = AutocompleteType.BusinessName },
                    new Autocomplete { Text = "Product", Type = AutocompleteType.ItemName }
                }
            };
            var payers = new[] { Mock.Requests.Payer() };
            var registerExpense = Mock.Requests.RegisterSimpleExpense(payers: payers, businessName: "Business", description: "Product");
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(expected, _channel.Items);
        }

        [Fact]
        public async Task Post_RegisterSimpleExpenseWithDescriptionLongerThanLimit_DoesNotUpdatesAutocompleteIndex()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var expected = new List<IEnumerable<Autocomplete>> {
                new List<Autocomplete>
                {
                    new Autocomplete { Text = "Business", Type = AutocompleteType.BusinessName }
                }
            };
            var payers = new[] { Mock.Requests.Payer() };
            var registerExpense = Mock.Requests.RegisterSimpleExpense(payers: payers, businessName: "Business", description: "Product with a long name that exceeds the limit");
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(expected, _channel.Items);
        }

        [Fact]
        public async Task Post_RegisterSimpleExpenseWithNoPayers_ProducesBadRequests()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var registerExpense = Mock.Requests.RegisterSimpleExpense(payers: new RegisterExpensePayer[0]);
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public async Task Post_RegisterSimpleExpenseWithSelfExpenses_ProducesBadRequests()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payee = Mock.Models.Roommate();
            var payers = new[] { Mock.Requests.Payer(id: payee.Id) };
            var registerExpense = Mock.Requests.RegisterSimpleExpense(payeeId: payee.Id, payers: payers);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = payee;

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("PayeeId"));
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public async Task Post_RegisterSimpleExpense_PayerWithAmountAndMultiplier_ProducesBadRequests()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(amount: 1, multiplier: 1) };
            var registerExpense = Mock.Requests.RegisterSimpleExpense(payers: payers);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId );

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public async Task Post_RegisterSimpleExpense_ShouldMatchPayersWithRegisteredExpensePayers()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(), Mock.Requests.Payer() };
            var registerExpense = Mock.Requests.RegisterSimpleExpense(payers: payers);
            var expected = payers.Select(x => x.Id).ToList();
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            var created = Assert.IsType<CreatedAtActionResult>(result);
            var expense = Assert.IsAssignableFrom<SimpleExpense>(created.Value);
            Assert.Equal(expected, expense.Payers.Select(x => x.Id).ToList());
        }

        [Fact]
        public async Task Post_RegisterSimpleExpense_ShouldMatchTotalWithRegisteredExpenseTotal()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(), Mock.Requests.Payer() };
            var registerExpense = Mock.Requests.RegisterSimpleExpense(payers: payers, total: 200M);
            var expected = 200M;
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            var created = Assert.IsType<CreatedAtActionResult>(result);
            var expense = Assert.IsAssignableFrom<SimpleExpense>(created.Value);
            Assert.Equal(expected, expense.Total);
        }

        [Theory, MemberData(nameof(BalancesEven))]
        public async Task Post_RegisterSimpleExpenseWithEvenDistribution_UpdatesRoommatesBalances(decimal total, Roommate roommate, Roommate[] roommates, (decimal payeeBalance, decimal[] payersBalances) expected)
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = roommates.Select(x => Mock.Requests.Payer(id: x.Id)).ToArray() ;
            var registerExpense = Mock.Requests.RegisterSimpleExpense(payeeId: roommate.Id, payers: payers, total: total);
            _roommates.Roommates = roommates;
            _roommates.Roommate = roommate;

            // act
            var result = (await controller.Post(registerExpense)).Result;
            var actual = (_roommates.Roommate.Balance, _roommates.Roommates.Select(x => x.Balance).ToArray());

            // assert
            Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(expected.payeeBalance, actual.Balance);
            Assert.Equal(expected.payersBalances, actual.Item2);
        }

        [Theory, MemberData(nameof(BalancesCustom))]
        public async Task Post_RegisterSimpleExpenseWithCustomDistribution_UpdatesRoommatesBalances(decimal total, Roommate roommate, (decimal amount, Roommate roommate)[] roommates, (decimal payeeBalance, decimal[] payersBalances) expected)
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = roommates.Select(x => Mock.Requests.Payer(id: x.roommate.Id, amount: x.amount)).ToArray();
            var registerExpense = Mock.Requests.RegisterSimpleExpense(payeeId: roommate.Id, payers: payers, total: total, distribution: ExpenseDistribution.Custom);
            _roommates.Roommates = roommates.Select(x => x.roommate).ToList();
            _roommates.Roommate = roommate;

            // act
            var result = (await controller.Post(registerExpense)).Result;
            var actual = (_roommates.Roommate.Balance, _roommates.Roommates.Select(x => x.Balance).ToArray());

            // assert
            Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(expected.payeeBalance, actual.Balance);
            Assert.Equal(expected.payersBalances, actual.Item2);
        }

        [Theory, MemberData(nameof(BalancesProportional))]
        public async Task Post_RegisterSimpleExpenseWithProportionalDistribution_UpdatesRoommatesBalances(decimal total, Roommate roommate, (double multiplier, Roommate roommate)[] roommates, (decimal payeeBalance, decimal[] payersBalances) expected)
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = roommates.Select(x => Mock.Requests.Payer(id: x.roommate.Id, multiplier: x.multiplier)).ToArray();
            var registerExpense = Mock.Requests.RegisterSimpleExpense(payeeId: roommate.Id, payers: payers, total: total, distribution: ExpenseDistribution.Proportional);
            _roommates.Roommates = roommates.Select(x => x.roommate).ToList();
            _roommates.Roommate = roommate;

            // act
            var result = (await controller.Post(registerExpense)).Result;
            var actual = (_roommates.Roommate.Balance, _roommates.Roommates.Select(x => x.Balance).ToArray());

            // assert
            Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(expected.payeeBalance, actual.Balance);
            Assert.Equal(expected.payersBalances, actual.Item2);
        }

        [Fact]
        public async Task Post_RegisterSimpleExpense_CustomDistributionWithNullAmount_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(amount: null), Mock.Requests.Payer(amount: null) };
            var registerExpense = Mock.Requests.RegisterSimpleExpense(payers: payers, distribution: ExpenseDistribution.Custom);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public async Task Post_RegisterSimpleExpense_CustomDistributionWithNegativeAmount_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(amount: -2), Mock.Requests.Payer(amount: -9) };
            var registerExpense = Mock.Requests.RegisterSimpleExpense(payers: payers, distribution: ExpenseDistribution.Custom);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public async Task Post_RegisterSimpleExpense_ProportionalDistributionWithNullAmount_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(multiplier: null), Mock.Requests.Payer(multiplier: null) };
            var registerExpense = Mock.Requests.RegisterSimpleExpense(payers: payers, distribution: ExpenseDistribution.Proportional);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public async Task Post_RegisterSimpleExpense_ProportionalDistributionWithNegativeAmount_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(multiplier: -.2), Mock.Requests.Payer(multiplier: 1.2) };
            var registerExpense = Mock.Requests.RegisterSimpleExpense(payers: payers, distribution: ExpenseDistribution.Proportional);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public async Task Post_RegisterSimpleExpense_ProportionalDistributionPayerWithMultiplierGreaterThanOne_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(multiplier: 1.2), Mock.Requests.Payer(multiplier: .2) };
            var registerExpense = Mock.Requests.RegisterSimpleExpense(payers: payers, distribution: ExpenseDistribution.Proportional);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public async Task Post_RegisterSimpleExpense_ProportionalDistributionWithMultiplierSumGreaterThanOne_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(multiplier: .5), Mock.Requests.Payer(multiplier: .9) };
            var registerExpense = Mock.Requests.RegisterSimpleExpense(payers: payers, distribution: ExpenseDistribution.Proportional);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public async Task Post_RegisterSimpleExpense_ProportionalDistributionWithMultiplierSumLowerThanOne_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(multiplier: .5), Mock.Requests.Payer(multiplier: .4) };
            var registerExpense = Mock.Requests.RegisterSimpleExpense(payers: payers, distribution: ExpenseDistribution.Proportional);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Delete_InvalidSimpleExpense_ProducesNotFoundResult()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);

            // act
            var result = controller.Delete("");

            // assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void Delete_SimpleExpense_RestoresRoommatesBalances()
        {
            // arrange
            var expected = new[] { 0M, 0M, 0M };
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var roommates = new[] { Mock.Models.Roommate(balance: -100), Mock.Models.Roommate(balance: 50), Mock.Models.Roommate(balance: 50) };
            var payee = Mock.Models.Payee(id: roommates[0].Id);
            var payers = roommates.Skip(1).Select(x => Mock.Models.Payer(id: x.Id, amount: 50)).ToArray();
            var expense = Mock.Models.SimpleExpense(total: 100, payers: payers, payee: payee);
            _expenses.Expense = expense;
            _expenses.SuccessfulDelete = true;
            _roommates.Roommate = roommates[0];
            _roommates.Roommates = roommates.Skip(1);

            // act
            var result = controller.Delete(expense.Id);

            // assert
            Assert.IsType<NoContentResult>(result);
            Assert.Equal(expected, roommates.Select(x => x.Balance).ToArray());
        }

        [Fact]
        public void Delete_SimpleExpense_RollbackRoommatesBalances_WhenUnsuccessfulDelete()
        {
            // arrange
            var expected = new[] { -100M, 50M, 50M };
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var roommates = new[] { Mock.Models.Roommate(balance: -100), Mock.Models.Roommate(balance: 50), Mock.Models.Roommate(balance: 50) };
            var payee = Mock.Models.Payee(id: roommates[0].Id);
            var payers = roommates.Skip(1).Select(x => Mock.Models.Payer(id: x.Id, amount: 50)).ToArray();
            var expense = Mock.Models.SimpleExpense(total: 100, payers: payers, payee: payee);
            _expenses.Expense = expense;
            _expenses.SuccessfulDelete = false;
            _roommates.Roommate = roommates[0];
            _roommates.Roommates = roommates.Skip(1);

            // act
            Action action = () => controller.Delete(expense.Id);

            // assert
            Assert.Throws<ApplicationException>(action);
            Assert.Equal(expected, roommates.Select(x => x.Balance).ToArray());
        }

        [Fact]
        public void Delete_SimpleExpenseWithPayments_ProducesBadRequests()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var roommates = new[] { Mock.Models.Roommate(balance: -100), Mock.Models.Roommate(balance: 50), Mock.Models.Roommate(balance: 50) };
            var expense = Mock.Models.SimpleExpense(payments: new[] { Mock.Models.PaymentSummary() });
            _expenses.Expense = expense;
            _roommates.Roommate = roommates[0];
            _roommates.Roommates = roommates.Skip(1);

            // act
            var result = controller.Delete(expense.Id);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payments"));
        }

        [Fact]
        public void Put_SimpleExpenseWithInvalidExpenseId_ProducesNotFoundResult()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);

            // act
            var result = controller.Put("", Mock.Requests.RegisterSimpleExpense());

            // assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void Put_SimpleExpenseWithNoPayers_ProducesBadRequests()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var registerExpense = Mock.Requests.RegisterSimpleExpense(payers: new RegisterExpensePayer[0]);
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);
            _expenses.Expense = Mock.Models.SimpleExpense();

            // act
            var result = controller.Put(_expenses.Expense.Id, registerExpense);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Put_SimpleExpenseWithSelfExpenses_ProducesBadRequests()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payee = Mock.Models.Roommate();
            var payers = new[] { Mock.Requests.Payer(id: payee.Id) };
            var registerExpense = Mock.Requests.RegisterSimpleExpense(payeeId: payee.Id, payers: payers);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = payee;
            _expenses.Expense = Mock.Models.SimpleExpense();

            // act
            var result = controller.Put(_expenses.Expense.Id, registerExpense);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("PayeeId"));
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Put_SimpleExpense_PayerWithAmountAndMultiplier_ProducesBadRequests()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(amount: 1, multiplier: 1) };
            var registerExpense = Mock.Requests.RegisterSimpleExpense(payers: payers);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);
            _expenses.Expense = Mock.Models.SimpleExpense();

            // act
            var result = controller.Put(_expenses.Expense.Id, registerExpense);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Put_SimpleExpenseWithCustomDistributionAndNullAmount_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(amount: null), Mock.Requests.Payer(amount: null) };
            var registerExpense = Mock.Requests.RegisterSimpleExpense(payers: payers, distribution: ExpenseDistribution.Custom);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);
            _expenses.Expense = Mock.Models.SimpleExpense();

            // act
            var result = controller.Put(_expenses.Expense.Id, registerExpense);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Put_SimpleExpenseWithCustomDistributionAndNegativeAmount_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(amount: -2), Mock.Requests.Payer(amount: -9) };
            var registerExpense = Mock.Requests.RegisterSimpleExpense(payers: payers, distribution: ExpenseDistribution.Custom);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);
            _expenses.Expense = Mock.Models.SimpleExpense();

            // act
            var result = controller.Put(_expenses.Expense.Id, registerExpense);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Put_SimpleExpenseWithProportionalDistributionAndNullAmount_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(multiplier: null), Mock.Requests.Payer(multiplier: null) };
            var registerExpense = Mock.Requests.RegisterSimpleExpense(payers: payers, distribution: ExpenseDistribution.Proportional);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);
            _expenses.Expense = Mock.Models.SimpleExpense();

            // act
            var result = controller.Put(_expenses.Expense.Id, registerExpense);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Put_SimpleExpenseWithProportionalDistributionAndNegativeAmount_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(multiplier: -.2), Mock.Requests.Payer(multiplier: 1.2) };
            var registerExpense = Mock.Requests.RegisterSimpleExpense(payers: payers, distribution: ExpenseDistribution.Proportional);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);
            _expenses.Expense = Mock.Models.SimpleExpense();

            // act
            var result = controller.Put(_expenses.Expense.Id, registerExpense);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Put_SimpleExpenseWithProportionalDistributionPayerAndMultiplierGreaterThanOne_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(multiplier: 1.2), Mock.Requests.Payer(multiplier: .2) };
            var registerExpense = Mock.Requests.RegisterSimpleExpense(payers: payers, distribution: ExpenseDistribution.Proportional);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);
            _expenses.Expense = Mock.Models.SimpleExpense();

            // act
            var result = controller.Put(_expenses.Expense.Id, registerExpense);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Put_SimpleExpenseWithProportionalDistributionAndMultiplierSumGreaterThanOne_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(multiplier: .5), Mock.Requests.Payer(multiplier: .9) };
            var registerExpense = Mock.Requests.RegisterSimpleExpense(payers: payers, distribution: ExpenseDistribution.Proportional);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);
            _expenses.Expense = Mock.Models.SimpleExpense();

            // act
            var result = controller.Put(_expenses.Expense.Id, registerExpense);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Put_SimpleExpenseWithProportionalDistributionAndMultiplierSumLowerThanOne_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(multiplier: .5), Mock.Requests.Payer(multiplier: .4) };
            var registerExpense = Mock.Requests.RegisterSimpleExpense(payers: payers, distribution: ExpenseDistribution.Proportional);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);
            _expenses.Expense = Mock.Models.SimpleExpense();

            // act
            var result = controller.Put(_expenses.Expense.Id, registerExpense);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Put_SimpleExpenseWithPayments_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(), Mock.Requests.Payer() };
            var registerExpense = Mock.Requests.RegisterSimpleExpense(payers: payers);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);
            _expenses.Expense = Mock.Models.SimpleExpense(payments: new[] { Mock.Models.PaymentSummary() });

            // act
            var result = controller.Put(_expenses.Expense.Id, registerExpense);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payments"));
        }

        [Fact]
        public void Put_SimpleExpense_ShouldMatchTotalWithRegisteredExpenseTotal()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(), Mock.Requests.Payer() };
            var registerExpense = Mock.Requests.RegisterSimpleExpense(payers: payers, total: 200M);
            var expected = 200M;
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);
            _expenses.SuccessfulUpdate = true;
            _expenses.Expense = Mock.Models.SimpleExpense(
                total: 0,
                payee: Mock.Models.Payee(id: registerExpense.PayeeId),
                payers: payers.Select(x => Mock.Models.Payer(id: x.Id, amount: 0)).ToArray()
            );

            // act
            var result = controller.Put(_expenses.Expense.Id, registerExpense);

            // assert
            var created = Assert.IsType<NoContentResult>(result);
            var expense = Assert.IsAssignableFrom<SimpleExpense>(_expenses.Expense);
            Assert.Equal(expected, expense.Total);
        }

        [Theory, MemberData(nameof(BalancesEven))]
        public void Put_SimpleExpenseWithEvenDistribution_UpdatesRoommatesBalances(decimal total, Roommate roommate, Roommate[] roommates, (decimal payeeBalance, decimal[] payersBalances) expected)
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = roommates.Select(x => Mock.Requests.Payer(id: x.Id)).ToArray();
            var registerExpense = Mock.Requests.RegisterSimpleExpense(payeeId: roommate.Id, payers: payers, total: total);
            _roommates.Roommates = roommates;
            _roommates.Roommate = roommate;
            _expenses.SuccessfulUpdate = true;
            _expenses.Expense = Mock.Models.SimpleExpense(
                total: 0,
                payee: Mock.Models.Payee(id: registerExpense.PayeeId),
                payers: payers.Select(x => Mock.Models.Payer(id: x.Id, amount: 0)).ToArray()
            );

            // act
            var result = controller.Put(_expenses.Expense.Id, registerExpense);
            var actual = (_roommates.Roommate.Balance, _roommates.Roommates.Select(x => x.Balance).ToArray());

            // assert
            Assert.IsType<NoContentResult>(result);
            Assert.Equal(expected.payeeBalance, actual.Balance);
            Assert.Equal(expected.payersBalances, actual.Item2);
        }

        [Theory, MemberData(nameof(BalancesCustom))]
        public void Put_SimpleExpenseWithCustomDistribution_UpdatesRoommatesBalances(decimal total, Roommate roommate, (decimal amount, Roommate roommate)[] roommates, (decimal payeeBalance, decimal[] payersBalances) expected)
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = roommates.Select(x => Mock.Requests.Payer(id: x.roommate.Id, amount: x.amount)).ToArray();
            var registerExpense = Mock.Requests.RegisterSimpleExpense(payeeId: roommate.Id, payers: payers, total: total, distribution: ExpenseDistribution.Custom);
            _roommates.Roommates = roommates.Select(x => x.roommate).ToList();
            _roommates.Roommate = roommate;
            _expenses.SuccessfulUpdate = true;
            _expenses.Expense = Mock.Models.SimpleExpense(
                total: 0,
                payee: Mock.Models.Payee(id: registerExpense.PayeeId),
                payers: payers.Select(x => Mock.Models.Payer(id: x.Id, amount: 0)).ToArray()
            );

            // act
            var result = controller.Put(_expenses.Expense.Id, registerExpense);
            var actual = (_roommates.Roommate.Balance, _roommates.Roommates.Select(x => x.Balance).ToArray());

            // assert
            Assert.IsType<NoContentResult>(result);
            Assert.Equal(expected.payeeBalance, actual.Balance);
            Assert.Equal(expected.payersBalances, actual.Item2);
        }

        [Theory, MemberData(nameof(BalancesProportional))]
        public void Put_SimpleExpenseWithProportionalDistribution_UpdatesRoommatesBalances(decimal total, Roommate roommate, (double multiplier, Roommate roommate)[] roommates, (decimal payeeBalance, decimal[] payersBalances) expected)
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = roommates.Select(x => Mock.Requests.Payer(id: x.roommate.Id, multiplier: x.multiplier)).ToArray();
            var registerExpense = Mock.Requests.RegisterSimpleExpense(payeeId: roommate.Id, payers: payers, total: total, distribution: ExpenseDistribution.Proportional);
            _roommates.Roommates = roommates.Select(x => x.roommate).ToList();
            _roommates.Roommate = roommate;
            _expenses.SuccessfulUpdate = true;
            _expenses.Expense = Mock.Models.SimpleExpense(
                total: 0,
                payee: Mock.Models.Payee(id: registerExpense.PayeeId),
                payers: payers.Select(x => Mock.Models.Payer(id: x.Id, amount: 0)).ToArray()
            );

            // act
            var result = controller.Put(_expenses.Expense.Id, registerExpense);
            var actual = (_roommates.Roommate.Balance, _roommates.Roommates.Select(x => x.Balance).ToArray());

            // assert
            Assert.IsType<NoContentResult>(result);
            Assert.Equal(expected.payeeBalance, actual.Balance);
            Assert.Equal(expected.payersBalances, actual.Item2);
        }

        [Fact]
        public void Put_SimpleExpense_ShouldMatchPayersWithRegisteredExpensePayers()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(), Mock.Requests.Payer() };
            var registerExpense = Mock.Requests.RegisterSimpleExpense(payers: payers);
            var expected = payers.Select(x => x.Id).ToList();
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);
            _expenses.SuccessfulUpdate = true;
            _expenses.Expense = Mock.Models.SimpleExpense(
                total: 0,
                payee: Mock.Models.Payee(id: registerExpense.PayeeId),
                payers: payers.Select(x => Mock.Models.Payer(id: x.Id, amount: 0)).ToArray()
            );

            // act
            var result = controller.Put(_expenses.Expense.Id, registerExpense);

            // assert
            var created = Assert.IsType<NoContentResult>(result);
            var expense = Assert.IsAssignableFrom<SimpleExpense>(_expenses.Expense);
            Assert.Equal(expected, expense.Payers.Select(x => x.Id).ToList());
        }

        [Fact]
        public void Put_SimpleExpense_ReplacesExistingExpense()
        {
            // arrange
            var expected = "Chinese Company";
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(), Mock.Requests.Payer() };
            var registerExpense = Mock.Requests.RegisterSimpleExpense(businessName: expected, payers: payers);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);
            _expenses.SuccessfulUpdate = true;
            _expenses.Expense = Mock.Models.SimpleExpense(
                total: 0,
                payee: Mock.Models.Payee(id: registerExpense.PayeeId),
                payers: payers.Select(x => Mock.Models.Payer(id: x.Id, amount: 0)).ToArray()
            );

            // act
            var result = controller.Put(_expenses.Expense.Id, registerExpense);

            // assert
            Assert.IsType<NoContentResult>(result);
            Assert.Equal(expected, _expenses.Expense.BusinessName);
        }

        [Fact]
        public void Put_SimpleExpense_UpdatesBalances()
        {
            // arrange
            var expected = (-100M, new[] { 60M, 40M });
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(amount: 60), Mock.Requests.Payer(amount: 40) };
            var registerExpense = Mock.Requests.RegisterSimpleExpense(total: 100, payers: payers, distribution: ExpenseDistribution.Custom);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id, balance: 100)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId, balance: -200);
            _expenses.SuccessfulUpdate = true;
            _expenses.Expense = Mock.Models.SimpleExpense(
                total: 200,
                payee: Mock.Models.Payee(id: registerExpense.PayeeId),
                payers: payers.Select(x => Mock.Models.Payer(id: x.Id, amount: 100)).ToArray()
            );

            // act
            var result = controller.Put(_expenses.Expense.Id, registerExpense);

            // assert
            Assert.IsType<NoContentResult>(result);
            Assert.Equal(expected.Item1, _roommates.Roommate.Balance);
            Assert.Equal(expected.Item2, _roommates.Roommates.Select(x => x.Balance).ToArray());
        }

        [Fact]
        public void Put_SimpleExpense_RollbackBalancesAfterFailure()
        {
            // arrange
            var expected = (-200M, new[] { 100M, 100M });
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(amount: 60), Mock.Requests.Payer(amount: 40) };
            var registerExpense = Mock.Requests.RegisterSimpleExpense(total: 100, payers: payers, distribution: ExpenseDistribution.Custom);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id, balance: 100)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId, balance: -200);
            _expenses.SuccessfulUpdate = false;
            _expenses.Expense = Mock.Models.SimpleExpense(
                total: 200,
                payee: Mock.Models.Payee(id: registerExpense.PayeeId),
                payers: payers.Select(x => Mock.Models.Payer(id: x.Id, amount: 100)).ToArray()
            );

            // act
            Action action = () => controller.Put(_expenses.Expense.Id, registerExpense);

            // assert
            Assert.Throws<ApplicationException>(action);
            Assert.Equal(expected.Item1, _roommates.Roommate.Balance);
            Assert.Equal(expected.Item2, _roommates.Roommates.Select(x => x.Balance).ToArray());
        }

        [Fact]
        public void Patch_SimpleExpenseWithInvalidExpenseId_ProducesNotFoundResult()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var patch = new JsonPatchDocument<RegisterExpense>();

            // act
            var result = controller.Patch("", patch);

            // assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void Patch_SimpleExpenseWithNoPayers_ProducesBadRequests()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var patch = new JsonPatchDocument<RegisterExpense>();
            patch.Replace(x => x.Payers, new RegisterExpensePayer[0]);
            _expenses.Expense = Mock.Models.SimpleExpense();
            _roommates.Roommate = Mock.Models.Roommate(id: _expenses.Expense.Payee.Id);

            // act
            var result = controller.Patch(_expenses.Expense.Id, patch);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Patch_SimpleExpenseWithSelfExpenses_ProducesBadRequests()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payee = Mock.Models.Roommate();
            var payers = new[] { Mock.Requests.Payer(id: payee.Id) };
            var patch = new JsonPatchDocument<RegisterExpense>();
            patch.Replace(x => x.Payers, payers);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = payee;
            _expenses.Expense = Mock.Models.SimpleExpense(payee: Mock.Models.Payee(id: payee.Id));

            // act
            var result = controller.Patch(_expenses.Expense.Id, patch);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("PayeeId"));
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Patch_SimpleExpense_PayerWithAmountAndMultiplier_ProducesBadRequests()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(amount: 1, multiplier: 1) };
            var patch = new JsonPatchDocument<RegisterExpense>();
            patch.Replace(x => x.Payers, payers);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _expenses.Expense = Mock.Models.SimpleExpense();
            _roommates.Roommate = Mock.Models.Roommate(id: _expenses.Expense.Payee.Id);

            // act
            var result = controller.Patch(_expenses.Expense.Id, patch);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Patch_SimpleExpenseWithCustomDistributionAndNullAmount_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(amount: null), Mock.Requests.Payer(amount: null) };
            var patch = new JsonPatchDocument<RegisterExpense>();
            patch.Replace(x => x.Payers, payers);
            patch.Replace(x => x.Distribution, ExpenseDistribution.Custom);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _expenses.Expense = Mock.Models.SimpleExpense();
            _roommates.Roommate = Mock.Models.Roommate(id: _expenses.Expense.Payee.Id);

            // act
            var result = controller.Patch(_expenses.Expense.Id, patch);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Patch_SimpleExpenseWithNullAmountAndExistingCustomDistribution_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(amount: null), Mock.Requests.Payer(amount: null) };
            var patch = new JsonPatchDocument<RegisterExpense>();
            patch.Replace(x => x.Payers, payers);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _expenses.Expense = Mock.Models.SimpleExpense(distribution: ExpenseDistribution.Custom);
            _roommates.Roommate = Mock.Models.Roommate(id: _expenses.Expense.Payee.Id);

            // act
            var result = controller.Patch(_expenses.Expense.Id, patch);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Patch_SimpleExpenseWithCustomDistributionAndNegativeAmount_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(amount: -2), Mock.Requests.Payer(amount: -9) };
            var patch = new JsonPatchDocument<RegisterExpense>();
            patch.Replace(x => x.Payers, payers);
            patch.Replace(x => x.Distribution, ExpenseDistribution.Custom);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _expenses.Expense = Mock.Models.SimpleExpense();
            _roommates.Roommate = Mock.Models.Roommate(id: _expenses.Expense.Payee.Id);

            // act
            var result = controller.Patch(_expenses.Expense.Id, patch);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Patch_SimpleExpenseWithProportionalDistributionAndNullAmount_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(multiplier: null), Mock.Requests.Payer(multiplier: null) };
            var patch = new JsonPatchDocument<RegisterExpense>();
            patch.Replace(x => x.Payers, payers);
            patch.Replace(x => x.Distribution, ExpenseDistribution.Proportional);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _expenses.Expense = Mock.Models.SimpleExpense();
            _roommates.Roommate = Mock.Models.Roommate(id: _expenses.Expense.Payee.Id);

            // act
            var result = controller.Patch(_expenses.Expense.Id, patch);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Patch_SimpleExpenseWithProportionalDistributionAndNegativeAmount_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(multiplier: -.2), Mock.Requests.Payer(multiplier: 1.2) };
            var patch = new JsonPatchDocument<RegisterExpense>();
            patch.Replace(x => x.Payers, payers);
            patch.Replace(x => x.Distribution, ExpenseDistribution.Proportional);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _expenses.Expense = Mock.Models.SimpleExpense();
            _roommates.Roommate = Mock.Models.Roommate(id: _expenses.Expense.Payee.Id);

            // act
            var result = controller.Patch(_expenses.Expense.Id, patch);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Patch_SimpleExpenseWithProportionalDistributionPayerAndMultiplierGreaterThanOne_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(multiplier: 1.2), Mock.Requests.Payer(multiplier: .2) };
            var patch = new JsonPatchDocument<RegisterExpense>();
            patch.Replace(x => x.Payers, payers);
            patch.Replace(x => x.Distribution, ExpenseDistribution.Proportional);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _expenses.Expense = Mock.Models.SimpleExpense();
            _roommates.Roommate = Mock.Models.Roommate(id: _expenses.Expense.Payee.Id);

            // act
            var result = controller.Patch(_expenses.Expense.Id, patch);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Patch_SimpleExpenseWithProportionalDistributionAndMultiplierSumGreaterThanOne_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(multiplier: .5), Mock.Requests.Payer(multiplier: .9) };
            var patch = new JsonPatchDocument<RegisterExpense>();
            patch.Replace(x => x.Payers, payers);
            patch.Replace(x => x.Distribution, ExpenseDistribution.Proportional);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _expenses.Expense = Mock.Models.SimpleExpense();
            _roommates.Roommate = Mock.Models.Roommate(id: _expenses.Expense.Payee.Id);

            // act
            var result = controller.Patch(_expenses.Expense.Id, patch);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Patch_SimpleExpenseWithProportionalDistributionAndMultiplierSumLowerThanOne_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(multiplier: .5), Mock.Requests.Payer(multiplier: .4) };
            var patch = new JsonPatchDocument<RegisterExpense>();
            patch.Replace(x => x.Payers, payers);
            patch.Replace(x => x.Distribution, ExpenseDistribution.Proportional);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _expenses.Expense = Mock.Models.SimpleExpense();
            _roommates.Roommate = Mock.Models.Roommate(id: _expenses.Expense.Payee.Id);

            // act
            var result = controller.Patch(_expenses.Expense.Id, patch);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Patch_SimpleExpenseWithPayments_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(), Mock.Requests.Payer() };
            var patch = new JsonPatchDocument<RegisterExpense>();
            patch.Replace(x => x.Payers, payers);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _expenses.Expense = Mock.Models.SimpleExpense(payments: new[] { Mock.Models.PaymentSummary() });
            _roommates.Roommate = Mock.Models.Roommate(id: _expenses.Expense.Payee.Id);

            // act
            var result = controller.Patch(_expenses.Expense.Id, patch);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payments"));
        }

        [Fact]
        public void Patch_SimpleExpense_ShouldMatchTotalWithRegisteredExpenseTotal()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(), Mock.Requests.Payer() };
            var patch = new JsonPatchDocument<RegisterExpense>();
            patch.Replace(x => x.Payers, payers);
            patch.Replace(x => x.Total, 200M);
            var expected = 200M;
            var payeeId = "payee-identifier";
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: payeeId);
            _expenses.SuccessfulUpdate = true;
            _expenses.Expense = Mock.Models.SimpleExpense(
                total: 0,
                payee: Mock.Models.Payee(id: payeeId),
                payers: payers.Select(x => Mock.Models.Payer(id: x.Id, amount: 0)).ToArray()
            );

            // act
            var result = controller.Patch(_expenses.Expense.Id, patch);

            // assert
            var created = Assert.IsType<NoContentResult>(result);
            var expense = Assert.IsAssignableFrom<SimpleExpense>(_expenses.Expense);
            Assert.Equal(expected, expense.Total);
        }

        [Theory, MemberData(nameof(BalancesEven))]
        public void Patch_SimpleExpenseWithEvenDistribution_UpdatesRoommatesBalances(decimal total, Roommate roommate, Roommate[] roommates, (decimal payeeBalance, decimal[] payersBalances) expected)
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = roommates.Select(x => Mock.Requests.Payer(id: x.Id)).ToArray();
            var patch = new JsonPatchDocument<RegisterExpense>();
            patch.Replace(x => x.Payers, payers);
            patch.Replace(x => x.Total, total);
            _roommates.Roommates = roommates;
            _roommates.Roommate = roommate;
            _expenses.SuccessfulUpdate = true;
            _expenses.Expense = Mock.Models.SimpleExpense(
                total: 0,
                payee: Mock.Models.Payee(id: roommate.Id),
                payers: payers.Select(x => Mock.Models.Payer(id: x.Id, amount: 0)).ToArray()
            );

            // act
            var result = controller.Patch(_expenses.Expense.Id, patch);
            var actual = (_roommates.Roommate.Balance, _roommates.Roommates.Select(x => x.Balance).ToArray());

            // assert
            Assert.IsType<NoContentResult>(result);
            Assert.Equal(expected.payeeBalance, actual.Balance);
            Assert.Equal(expected.payersBalances, actual.Item2);
        }

        [Theory, MemberData(nameof(BalancesCustom))]
        public void Patch_SimpleExpenseWithCustomDistribution_UpdatesRoommatesBalances(decimal total, Roommate roommate, (decimal amount, Roommate roommate)[] roommates, (decimal payeeBalance, decimal[] payersBalances) expected)
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = roommates.Select(x => Mock.Requests.Payer(id: x.roommate.Id, amount: x.amount)).ToArray();
            var patch = new JsonPatchDocument<RegisterExpense>();
            patch.Replace(x => x.Payers, payers);
            patch.Replace(x => x.Total, total);
            patch.Replace(x => x.Distribution, ExpenseDistribution.Custom);
            _roommates.Roommates = roommates.Select(x => x.roommate).ToList();
            _roommates.Roommate = roommate;
            _expenses.SuccessfulUpdate = true;
            _expenses.Expense = Mock.Models.SimpleExpense(
                total: 0,
                payee: Mock.Models.Payee(id: roommate.Id),
                payers: payers.Select(x => Mock.Models.Payer(id: x.Id, amount: 0)).ToArray()
            );

            // act
            var result = controller.Patch(_expenses.Expense.Id, patch);
            var actual = (_roommates.Roommate.Balance, _roommates.Roommates.Select(x => x.Balance).ToArray());

            // assert
            Assert.IsType<NoContentResult>(result);
            Assert.Equal(expected.payeeBalance, actual.Balance);
            Assert.Equal(expected.payersBalances, actual.Item2);
        }

        [Theory, MemberData(nameof(BalancesProportional))]
        public void Patch_SimpleExpenseWithProportionalDistribution_UpdatesRoommatesBalances(decimal total, Roommate roommate, (double multiplier, Roommate roommate)[] roommates, (decimal payeeBalance, decimal[] payersBalances) expected)
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = roommates.Select(x => Mock.Requests.Payer(id: x.roommate.Id, multiplier: x.multiplier)).ToArray();
            var patch = new JsonPatchDocument<RegisterExpense>();
            patch.Replace(x => x.Payers, payers);
            patch.Replace(x => x.Total, total);
            patch.Replace(x => x.Distribution, ExpenseDistribution.Proportional);
            _roommates.Roommates = roommates.Select(x => x.roommate).ToList();
            _roommates.Roommate = roommate;
            _expenses.SuccessfulUpdate = true;
            _expenses.Expense = Mock.Models.SimpleExpense(
                total: 0,
                payee: Mock.Models.Payee(id: roommate.Id),
                payers: payers.Select(x => Mock.Models.Payer(id: x.Id, amount: 0)).ToArray()
            );

            // act
            var result = controller.Patch(_expenses.Expense.Id, patch);
            var actual = (_roommates.Roommate.Balance, _roommates.Roommates.Select(x => x.Balance).ToArray());

            // assert
            Assert.IsType<NoContentResult>(result);
            Assert.Equal(expected.payeeBalance, actual.Balance);
            Assert.Equal(expected.payersBalances, actual.Item2);
        }

        [Fact]
        public void Patch_SimpleExpense_ShouldMatchPayersWithRegisteredExpensePayers()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(), Mock.Requests.Payer() };
            var patch = new JsonPatchDocument<RegisterExpense>();
            patch.Replace(x => x.Payers, payers);
            var expected = payers.Select(x => x.Id).ToList();
            var payeeId = "payee-identifier";
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: payeeId);
            _expenses.SuccessfulUpdate = true;
            _expenses.Expense = Mock.Models.SimpleExpense(
                total: 0,
                payee: Mock.Models.Payee(id: payeeId),
                payers: payers.Select(x => Mock.Models.Payer(id: x.Id, amount: 0)).ToArray()
            );

            // act
            var result = controller.Patch(_expenses.Expense.Id, patch);

            // assert
            var created = Assert.IsType<NoContentResult>(result);
            var expense = Assert.IsAssignableFrom<SimpleExpense>(_expenses.Expense);
            Assert.Equal(expected, expense.Payers.Select(x => x.Id).ToList());
        }

        [Fact]
        public void Patch_SimpleExpense_UpdatesExistingExpense()
        {
            // arrange
            var expected = "Chinese Company";
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(), Mock.Requests.Payer() };
            var patch = new JsonPatchDocument<RegisterExpense>();
            patch.Replace(x => x.BusinessName, expected);
            patch.Replace(x => x.Payers, payers);
            var payeeId = "payee-identifier";
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: payeeId);
            _expenses.SuccessfulUpdate = true;
            _expenses.Expense = Mock.Models.SimpleExpense(
                total: 0,
                payee: Mock.Models.Payee(id: payeeId),
                payers: payers.Select(x => Mock.Models.Payer(id: x.Id, amount: 0)).ToArray()
            );

            // act
            var result = controller.Patch(_expenses.Expense.Id, patch);

            // assert
            Assert.IsType<NoContentResult>(result);
            Assert.Equal(expected, _expenses.Expense.BusinessName);
        }

        [Fact]
        public void Patch_SimpleExpense_UpdatesBalances()
        {
            // arrange
            var expected = (-100M, new[] { 60M, 40M });
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(amount: 60), Mock.Requests.Payer(amount: 40) };
            var patch = new JsonPatchDocument<RegisterExpense>();
            patch.Replace(x => x.Payers, payers);
            patch.Replace(x => x.Total, 100M);
            patch.Replace(x => x.Distribution, ExpenseDistribution.Custom);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id, balance: 100)).ToList();
            var payeeId = "payee-identifier";
            _roommates.Roommate = Mock.Models.Roommate(id: payeeId, balance: -200);
            _expenses.SuccessfulUpdate = true;
            _expenses.Expense = Mock.Models.SimpleExpense(
                total: 200,
                payee: Mock.Models.Payee(id: payeeId),
                payers: payers.Select(x => Mock.Models.Payer(id: x.Id, amount: 100)).ToArray()
            );

            // act
            var result = controller.Patch(_expenses.Expense.Id, patch);

            // assert
            Assert.IsType<NoContentResult>(result);
            Assert.Equal(expected.Item1, _roommates.Roommate.Balance);
            Assert.Equal(expected.Item2, _roommates.Roommates.Select(x => x.Balance).ToArray());
        }

        [Fact]
        public void Patch_SimpleExpense_RollbackBalancesAfterFailure()
        {
            // arrange
            var expected = (-200M, new[] { 100M, 100M });
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(amount: 60), Mock.Requests.Payer(amount: 40) };
            var patch = new JsonPatchDocument<RegisterExpense>();
            patch.Replace(x => x.Payers, payers);
            patch.Replace(x => x.Total, 100M);
            patch.Replace(x => x.Distribution, ExpenseDistribution.Custom);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id, balance: 100)).ToList();
            var payeeId = "payee-identifier";
            _roommates.Roommate = Mock.Models.Roommate(id: payeeId, balance: -200);
            _expenses.SuccessfulUpdate = false;
            _expenses.Expense = Mock.Models.SimpleExpense(
                total: 200,
                payee: Mock.Models.Payee(id: payeeId),
                payers: payers.Select(x => Mock.Models.Payer(id: x.Id, amount: 100)).ToArray()
            );

            // act
            Action action = () => controller.Patch(_expenses.Expense.Id, patch);

            // assert
            Assert.Throws<ApplicationException>(action);
            Assert.Equal(expected.Item1, _roommates.Roommate.Balance);
            Assert.Equal(expected.Item2, _roommates.Roommates.Select(x => x.Balance).ToArray());
        }
        #endregion

        #region Detailed Expense
        [Theory, MemberData(nameof(EvenDistributions))]
        public async Task Post_RegisterDetailedExpense_DistributesEvenExpense(decimal total, decimal[] expected)
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = expected.Select(x => Mock.Requests.Payer()).ToArray();
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers, distribution: ExpenseDistribution.Even, price: total) };
            var registerExpense = Mock.Requests.RegisterDetailedExpense(total: total, items: items);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            var created = Assert.IsType<CreatedAtActionResult>(result);
            var expense = Assert.IsAssignableFrom<DetailedExpense>(created.Value);
            var actual = expense.Items.Select(i => i.Payers.Select(p => p.Amount));
            Assert.Equal(new[] { expected }, actual);
        }

        [Theory, MemberData(nameof(CustomDistributions))]
        public async Task Post_RegisterDetailedExpense_DistributesCustomExpense(decimal total, decimal[] expected)
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = expected.Select(x => Mock.Requests.Payer(amount: x)).ToArray();
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers, distribution: ExpenseDistribution.Custom, price: total) };
            var registerExpense = Mock.Requests.RegisterDetailedExpense(total: total, items: items);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            var created = Assert.IsType<CreatedAtActionResult>(result);
            var expense = Assert.IsAssignableFrom<DetailedExpense>(created.Value);
            var actual = expense.Items.Select(i => i.Payers.Select(p => p.Amount));
            Assert.Equal(new[] { expected }, actual);
        }

        [Theory, MemberData(nameof(ProportionalDistributions))]
        public async Task Post_RegisterDetailedExpense_DistributesProportionalExpense(decimal total, double[] multipliers, decimal[] expected)
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = multipliers.Select(x => Mock.Requests.Payer(multiplier: x)).ToArray();
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers, distribution: ExpenseDistribution.Proportional, price: total) };
            var registerExpense = Mock.Requests.RegisterDetailedExpense(total: total, items: items);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            var created = Assert.IsType<CreatedAtActionResult>(result);
            var expense = Assert.IsAssignableFrom<DetailedExpense>(created.Value);
            var actual = expense.Items.Select(i => i.Payers.Select(p => p.Amount));
            Assert.Equal(new[] { expected }, actual);
        }

        [Fact]
        public async Task Post_RegisterDetailedExpenseWithIncorrectTotal_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var total = 2000M;
            var payers = new[] { Mock.Requests.Payer(amount: 120) };
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers, distribution: ExpenseDistribution.Custom, price: total) };
            var registerExpense = Mock.Requests.RegisterDetailedExpense(total: total, items: items);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.Equal(4, errors.Values.SelectMany(x => (string[])x).Count());
            Assert.True(errors.ContainsKey("Total"));
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public async Task Post_RegisterDetailedExpenseWithIncorrectTotalPerItem_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var total = 2000M;
            var payers = new[] { Mock.Requests.Payer(amount: total) };
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers, distribution: ExpenseDistribution.Custom, price: 120) };
            var registerExpense = Mock.Requests.RegisterDetailedExpense(total: total, items: items);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.Equal(4, errors.Values.SelectMany(x => (string[])x).Count());
            Assert.True(errors.ContainsKey("Total"));
            Assert.True(errors.ContainsKey("Items"));
        }

        [Fact]
        public async Task Post_RegisterDetailedExpense_UpdatesAutocompleteIndex()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var expected = new List<IEnumerable<Autocomplete>> {
                new List<Autocomplete>
                {
                    new Autocomplete { Text = "Business", Type = AutocompleteType.BusinessName },
                    new Autocomplete { Text = "Product", Type = AutocompleteType.ItemName }
                }
            };
            var payers = new[] { Mock.Requests.Payer() };
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers, name: "Product") };
            var registerExpense = Mock.Requests.RegisterDetailedExpense(businessName: "Business", items: items);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(expected, _channel.Items);
        }

        [Fact]
        public async Task Post_RegisterDetailedExpenseWithNoPayers_ProducesBadRequests()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var items = new[] { Mock.Requests.ExpenseItem(payers: new RegisterExpensePayer[0]) };
            var registerExpense = Mock.Requests.RegisterDetailedExpense(items: items);
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public async Task Post_RegisterDetailedExpenseWithSelfExpenses_ProducesBadRequests()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payee = Mock.Models.Roommate();
            var payers = new[] { Mock.Requests.Payer(id: payee.Id) };
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers) };
            var registerExpense = Mock.Requests.RegisterDetailedExpense(payeeId: payee.Id, items: items);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = payee;

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("PayeeId"));
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public async Task Post_RegisterDetailedExpense_PayerWithAmountAndMultiplier_ProducesBadRequests()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(amount: 1, multiplier: 1) };
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers) };
            var registerExpense = Mock.Requests.RegisterDetailedExpense(items: items);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public async Task Post_RegisterDetailedExpense_ShouldMatchPayersWithRegisteredExpensePayers()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(), Mock.Requests.Payer() };
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers) };
            var registerExpense = Mock.Requests.RegisterDetailedExpense(items: items);
            var expected = payers.Select(x => x.Id).ToList();
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            var created = Assert.IsType<CreatedAtActionResult>(result);
            var expense = Assert.IsAssignableFrom<DetailedExpense>(created.Value);
            var actual = expense.Items.Select(i => i.Payers.Select(p => p.Id)).ToList();
            Assert.Equal(new[] { expected }, actual);
        }

        [Fact]
        public async Task Post_RegisterDetailedExpense_ShouldMatchTotalWithRegisteredExpenseTotal()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(), Mock.Requests.Payer() };
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers, price: 100M), Mock.Requests.ExpenseItem(name: "TNT 2", payers: payers, price: 100M) };
            var registerExpense = Mock.Requests.RegisterDetailedExpense(items: items, total: 200M);
            var expected = 200M;
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            var created = Assert.IsType<CreatedAtActionResult>(result);
            var expense = Assert.IsAssignableFrom<DetailedExpense>(created.Value);
            Assert.Equal(expected, expense.Total);
        }

        [Theory, MemberData(nameof(BalancesEven))]
        public async Task Post_RegisterDetailedExpenseWithEvenDistribution_UpdatesRoommatesBalances(decimal total, Roommate roommate, Roommate[] roommates, (decimal payeeBalance, decimal[] payersBalances) expected)
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = roommates.Select(x => Mock.Requests.Payer(id: x.Id)).ToArray();
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers, price: total) };
            var registerExpense = Mock.Requests.RegisterDetailedExpense(payeeId: roommate.Id, items: items, total: total);
            _roommates.Roommates = roommates;
            _roommates.Roommate = roommate;

            // act
            var result = (await controller.Post(registerExpense)).Result;
            var actual = (_roommates.Roommate.Balance, _roommates.Roommates.Select(x => x.Balance).ToArray());

            // assert
            Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(expected.payeeBalance, actual.Balance);
            Assert.Equal(expected.payersBalances, actual.Item2);
        }

        [Theory, MemberData(nameof(BalancesCustom))]
        public async Task Post_RegisterDetailedExpenseWithCustomDistribution_UpdatesRoommatesBalances(decimal total, Roommate roommate, (decimal amount, Roommate roommate)[] roommates, (decimal payeeBalance, decimal[] payersBalances) expected)
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = roommates.Select(x => Mock.Requests.Payer(id: x.roommate.Id, amount: x.amount)).ToArray();
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers, price: total, distribution: ExpenseDistribution.Custom) };
            var registerExpense = Mock.Requests.RegisterDetailedExpense(payeeId: roommate.Id, items: items, total: total);
            _roommates.Roommates = roommates.Select(x => x.roommate).ToList();
            _roommates.Roommate = roommate;

            // act
            var result = (await controller.Post(registerExpense)).Result;
            var actual = (_roommates.Roommate.Balance, _roommates.Roommates.Select(x => x.Balance).ToArray());

            // assert
            Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(expected.payeeBalance, actual.Balance);
            Assert.Equal(expected.payersBalances, actual.Item2);
        }

        [Theory, MemberData(nameof(BalancesProportional))]
        public async Task Post_RegisterDetailedExpenseWithProportionalDistribution_UpdatesRoommatesBalances(decimal total, Roommate roommate, (double multiplier, Roommate roommate)[] roommates, (decimal payeeBalance, decimal[] payersBalances) expected)
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = roommates.Select(x => Mock.Requests.Payer(id: x.roommate.Id, multiplier: x.multiplier)).ToArray();
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers, price: total, distribution: ExpenseDistribution.Proportional) };
            var registerExpense = Mock.Requests.RegisterDetailedExpense(payeeId: roommate.Id, items: items, total: total);
            _roommates.Roommates = roommates.Select(x => x.roommate).ToList();
            _roommates.Roommate = roommate;

            // act
            var result = (await controller.Post(registerExpense)).Result;
            var actual = (_roommates.Roommate.Balance, _roommates.Roommates.Select(x => x.Balance).ToArray());

            // assert
            Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(expected.payeeBalance, actual.Balance);
            Assert.Equal(expected.payersBalances, actual.Item2);
        }

        [Fact]
        public async Task Post_RegisterDetailedExpense_CustomDistributionWithNullAmount_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(amount: null), Mock.Requests.Payer(amount: null) };
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers, distribution: ExpenseDistribution.Custom) };
            var registerExpense = Mock.Requests.RegisterDetailedExpense(items: items);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public async Task Post_RegisterDetailedExpense_CustomDistributionWithNegativeAmount_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(amount: -2), Mock.Requests.Payer(amount: -9) };
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers, distribution: ExpenseDistribution.Custom) };
            var registerExpense = Mock.Requests.RegisterDetailedExpense(items: items);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public async Task Post_RegisterDetailedExpense_ProportionalDistributionWithNullAmount_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(multiplier: null), Mock.Requests.Payer(multiplier: null) };
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers, distribution: ExpenseDistribution.Proportional) };
            var registerExpense = Mock.Requests.RegisterDetailedExpense(items: items);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public async Task Post_RegisterDetailedExpense_ProportionalDistributionWithNegativeAmount_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(multiplier: -.2), Mock.Requests.Payer(multiplier: 1.2) };
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers, distribution: ExpenseDistribution.Proportional) };
            var registerExpense = Mock.Requests.RegisterDetailedExpense(items: items);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public async Task Post_RegisterDetailedExpense_ProportionalDistributionPayerWithMultiplierGreaterThanOne_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(multiplier: 1.2), Mock.Requests.Payer(multiplier: .2) };
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers, distribution: ExpenseDistribution.Proportional) };
            var registerExpense = Mock.Requests.RegisterDetailedExpense(items: items);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public async Task Post_RegisterDetailedExpense_ProportionalDistributionWithMultiplierSumGreaterThanOne_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(multiplier: .5), Mock.Requests.Payer(multiplier: .9) };
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers, distribution: ExpenseDistribution.Proportional) };
            var registerExpense = Mock.Requests.RegisterDetailedExpense(items: items);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public async Task Post_RegisterDetailedExpense_ProportionalDistributionWithMultiplierSumLowerThanOne_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(multiplier: .5), Mock.Requests.Payer(multiplier: .4) };
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers, distribution: ExpenseDistribution.Proportional) };
            var registerExpense = Mock.Requests.RegisterDetailedExpense(items: items);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public async Task Post_RegisterDetailedExpenseWithMultipleItemsAndASelfExpense_ReturnsRegisteredExpense()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payee = Mock.Models.Roommate(balance: 0);
            var payer = Mock.Models.Roommate();
            var payers = new[] {
                Mock.Requests.Payer(id: payee.Id),
                Mock.Requests.Payer(id: payer.Id)
            };
            var items = new[] {
                Mock.Requests.ExpenseItem(payers: new[] { Mock.Requests.Payer(id: payee.Id) }),
                Mock.Requests.ExpenseItem(payers: payers)
            };
            var registerExpense = Mock.Requests.RegisterDetailedExpense(payeeId: payee.Id, items: items, total: items.Sum(x => x.Total));
            _roommates.Roommates = new[] { payee, payer }.ToList();
            _roommates.Roommate = payee;

            // act
            var result = (await controller.Post(registerExpense)).Result;

            // assert
            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.IsAssignableFrom<DetailedExpense>(created.Value);
            Assert.Equal(-0.5M, payee.Balance);
        }

        [Fact]
        public void Delete_DetailedExpense_RestoresRoommatesBalances()
        {
            // arrange
            var expected = new[] { 0M, 0M, 0M };
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var roommates = new[] { Mock.Models.Roommate(balance: -100), Mock.Models.Roommate(balance: 50), Mock.Models.Roommate(balance: 50) };
            var payee = Mock.Models.Payee(id: roommates[0].Id);
            var payers = roommates.Skip(1).Select(x => Mock.Models.Payer(id: x.Id, amount: 50)).ToArray();
            var items = new[] { Mock.Models.ExpenseItem(payers: payers, price: 100) };
            var expense = Mock.Models.DetailedExpense(total: 100, items: items, payee: payee);
            _expenses.Expense = expense;
            _expenses.SuccessfulDelete = true;
            _roommates.Roommate = roommates[0];
            _roommates.Roommates = roommates.Skip(1);

            // act
            var result = controller.Delete(expense.Id);

            // assert
            Assert.IsType<NoContentResult>(result);
            Assert.Equal(expected, roommates.Select(x => x.Balance).ToArray());
        }

        [Fact]
        public void Delete_DetailedExpenseWithPayments_ProducesBadRequests()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var roommates = new[] { Mock.Models.Roommate(balance: -100), Mock.Models.Roommate(balance: 50), Mock.Models.Roommate(balance: 50) };
            var expense = Mock.Models.DetailedExpense(payments: new[] { Mock.Models.PaymentSummary() });
            _expenses.Expense = expense;
            _roommates.Roommate = roommates[0];
            _roommates.Roommates = roommates.Skip(1);

            // act
            var result = controller.Delete(expense.Id);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payments"));
        }

        [Fact]
        public void Delete_DetailedExpense_RollbackRoommatesBalances_WhenUnsuccessfulDelete()
        {
            // arrange
            var expected = new[] { -100M, 50M, 50M };
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var roommates = new[] { Mock.Models.Roommate(balance: -100), Mock.Models.Roommate(balance: 50), Mock.Models.Roommate(balance: 50) };
            var payee = Mock.Models.Payee(id: roommates[0].Id);
            var payers = roommates.Skip(1).Select(x => Mock.Models.Payer(id: x.Id, amount: 50)).ToArray();
            var items = new[] { Mock.Models.ExpenseItem(payers: payers, price: 100) };
            var expense = Mock.Models.DetailedExpense(total: 100, items: items, payee: payee);
            _expenses.Expense = expense;
            _expenses.SuccessfulDelete = false;
            _roommates.Roommate = roommates[0];
            _roommates.Roommates = roommates.Skip(1);

            // act
            Action action = () => controller.Delete(expense.Id);

            // assert
            Assert.Throws<ApplicationException>(action);
            Assert.Equal(expected, roommates.Select(x => x.Balance).ToArray());
        }

        [Fact]
        public void Put_DetailedExpenseWithInvalidExpenseId_ProducesNotFoundResult()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);

            // act
            var result = controller.Put("", Mock.Requests.RegisterDetailedExpense());

            // assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void Put_DetailedExpenseWithNoPayers_ProducesBadRequests()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var registerExpense = Mock.Requests.RegisterDetailedExpense(items: new[] { Mock.Requests.ExpenseItem(payers: new RegisterExpensePayer[0]) });
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);
            _expenses.Expense = Mock.Models.DetailedExpense();

            // act
            var result = controller.Put(_expenses.Expense.Id, registerExpense);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Put_DetailedExpenseWithSelfExpenses_ProducesBadRequests()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payee = Mock.Models.Roommate();
            var payers = new[] { Mock.Requests.Payer(id: payee.Id) };
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers) };
            var registerExpense = Mock.Requests.RegisterDetailedExpense(payeeId: payee.Id, items: items);
            _roommates.Roommates = new[] { payee };
            _roommates.Roommate = payee;
            _expenses.Expense = Mock.Models.DetailedExpense();

            // act
            var result = controller.Put(_expenses.Expense.Id, registerExpense);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("PayeeId"));
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Put_DetailedExpense_PayerWithAmountAndMultiplier_ProducesBadRequests()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(amount: 1, multiplier: 1) };
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers) };
            var registerExpense = Mock.Requests.RegisterDetailedExpense(items: items);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);
            _expenses.Expense = Mock.Models.DetailedExpense();

            // act
            var result = controller.Put(_expenses.Expense.Id, registerExpense);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Put_DetailedExpenseWithCustomDistributionAndNullAmount_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(amount: null), Mock.Requests.Payer(amount: null) };
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers, distribution: ExpenseDistribution.Custom) };
            var registerExpense = Mock.Requests.RegisterDetailedExpense(items: items);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);
            _expenses.Expense = Mock.Models.DetailedExpense();

            // act
            var result = controller.Put(_expenses.Expense.Id, registerExpense);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Put_DetailedExpenseWithCustomDistributionAndNegativeAmount_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(amount: -2), Mock.Requests.Payer(amount: -9) };
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers, distribution: ExpenseDistribution.Custom) };
            var registerExpense = Mock.Requests.RegisterDetailedExpense(items: items);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);
            _expenses.Expense = Mock.Models.DetailedExpense();

            // act
            var result = controller.Put(_expenses.Expense.Id, registerExpense);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Put_DetailedWithProportionalDistributionAndNullAmount_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(multiplier: null), Mock.Requests.Payer(multiplier: null) };
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers, distribution: ExpenseDistribution.Proportional) };
            var registerExpense = Mock.Requests.RegisterDetailedExpense(items: items);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);
            _expenses.Expense = Mock.Models.DetailedExpense();

            // act
            var result = controller.Put(_expenses.Expense.Id, registerExpense);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Put_DetailedExpenseWithProportionalDistributionAndNegativeAmount_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(multiplier: -.2), Mock.Requests.Payer(multiplier: 1.2) };
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers, distribution: ExpenseDistribution.Proportional) };
            var registerExpense = Mock.Requests.RegisterDetailedExpense(items: items);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);
            _expenses.Expense = Mock.Models.DetailedExpense();

            // act
            var result = controller.Put(_expenses.Expense.Id, registerExpense);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Put_DetailedExpenseWithProportionalDistributionPayerAndMultiplierGreaterThanOne_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(multiplier: 1.2), Mock.Requests.Payer(multiplier: .2) };
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers, distribution: ExpenseDistribution.Proportional) };
            var registerExpense = Mock.Requests.RegisterDetailedExpense(items: items);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);
            _expenses.Expense = Mock.Models.DetailedExpense();

            // act
            var result = controller.Put(_expenses.Expense.Id, registerExpense);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Put_DetailedExpenseWithProportionalDistributionAndMultiplierSumGreaterThanOne_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(multiplier: .5), Mock.Requests.Payer(multiplier: .9) };
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers, distribution: ExpenseDistribution.Proportional) };
            var registerExpense = Mock.Requests.RegisterDetailedExpense(items: items);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);
            _expenses.Expense = Mock.Models.DetailedExpense();

            // act
            var result = controller.Put(_expenses.Expense.Id, registerExpense);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Put_DetailedExpenseWithProportionalDistributionAndMultiplierSumLowerThanOne_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(multiplier: .5), Mock.Requests.Payer(multiplier: .4) };
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers, distribution: ExpenseDistribution.Proportional) };
            var registerExpense = Mock.Requests.RegisterDetailedExpense(items: items);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);
            _expenses.Expense = Mock.Models.DetailedExpense();

            // act
            var result = controller.Put(_expenses.Expense.Id, registerExpense);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Put_DetailedExpenseWithPayments_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(), Mock.Requests.Payer() };
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers) };
            var registerExpense = Mock.Requests.RegisterDetailedExpense(items: items);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _roommates.Roommate = Mock.Models.Roommate(id: registerExpense.PayeeId);
            _expenses.Expense = Mock.Models.DetailedExpense(payments: new[] { Mock.Models.PaymentSummary() });

            // act
            var result = controller.Put(_expenses.Expense.Id, registerExpense);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payments"));
        }

        [Fact]
        public void Patch_DetailedExpenseWithInvalidExpenseId_ProducesNotFoundResult()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);

            // act
            var result = controller.Patch("", new JsonPatchDocument<RegisterExpense>());

            // assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void Patch_DetailedExpenseWithNoPayers_ProducesBadRequests()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var items = new[] { Mock.Requests.ExpenseItem(payers: new RegisterExpensePayer[0]) };
            var patch = new JsonPatchDocument<RegisterExpense>();
            patch.Replace(x => x.Items, items);
            _expenses.Expense = Mock.Models.DetailedExpense();
            _roommates.Roommate = Mock.Models.Roommate(id: _expenses.Expense.Payee.Id);

            // act
            var result = controller.Patch(_expenses.Expense.Id, patch);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Patch_DetailedExpenseWithSelfExpenses_ProducesBadRequests()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payee = Mock.Models.Roommate();
            var payers = new[] { Mock.Requests.Payer(id: payee.Id) };
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers) };
            var patch = new JsonPatchDocument<RegisterExpense>();
            patch.Replace(x => x.PayeeId, payee.Id);
            patch.Replace(x => x.Items, items);
            _roommates.Roommates = new[] { payee };
            _roommates.Roommate = payee;
            _expenses.Expense = Mock.Models.DetailedExpense();

            // act
            var result = controller.Patch(_expenses.Expense.Id, patch);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("PayeeId"));
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Patch_DetailedExpense_PayerWithAmountAndMultiplier_ProducesBadRequests()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(amount: 1, multiplier: 1) };
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers) };
            var patch = new JsonPatchDocument<RegisterExpense>();
            patch.Replace(x => x.Items, items);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _expenses.Expense = Mock.Models.DetailedExpense();
            _roommates.Roommate = Mock.Models.Roommate(id: _expenses.Expense.Payee.Id);

            // act
            var result = controller.Patch(_expenses.Expense.Id, patch);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Patch_DetailedExpenseWithCustomDistributionAndNullAmount_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(amount: null), Mock.Requests.Payer(amount: null) };
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers, distribution: ExpenseDistribution.Custom) };
            var patch = new JsonPatchDocument<RegisterExpense>();
            patch.Replace(x => x.Items, items);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _expenses.Expense = Mock.Models.DetailedExpense();
            _roommates.Roommate = Mock.Models.Roommate(id: _expenses.Expense.Payee.Id);

            // act
            var result = controller.Patch(_expenses.Expense.Id, patch);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Patch_DetailedExpenseWithCustomDistributionAndNegativeAmount_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(amount: -2), Mock.Requests.Payer(amount: -9) };
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers, distribution: ExpenseDistribution.Custom) };
            var patch = new JsonPatchDocument<RegisterExpense>();
            patch.Replace(x => x.Items, items);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _expenses.Expense = Mock.Models.DetailedExpense();
            _roommates.Roommate = Mock.Models.Roommate(id: _expenses.Expense.Payee.Id);

            // act
            var result = controller.Patch(_expenses.Expense.Id, patch);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Patch_DetailedWithProportionalDistributionAndNullAmount_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(multiplier: null), Mock.Requests.Payer(multiplier: null) };
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers, distribution: ExpenseDistribution.Proportional) };
            var patch = new JsonPatchDocument<RegisterExpense>();
            patch.Replace(x => x.Items, items);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _expenses.Expense = Mock.Models.DetailedExpense();
            _roommates.Roommate = Mock.Models.Roommate(id: _expenses.Expense.Payee.Id);

            // act
            var result = controller.Patch(_expenses.Expense.Id, patch);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Patch_DetailedExpenseWithProportionalDistributionAndNegativeAmount_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(multiplier: -.2), Mock.Requests.Payer(multiplier: 1.2) };
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers, distribution: ExpenseDistribution.Proportional) };
            var patch = new JsonPatchDocument<RegisterExpense>();
            patch.Replace(x => x.Items, items);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _expenses.Expense = Mock.Models.DetailedExpense();
            _roommates.Roommate = Mock.Models.Roommate(id: _expenses.Expense.Payee.Id);

            // act
            var result = controller.Patch(_expenses.Expense.Id, patch);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Patch_DetailedExpenseWithProportionalDistributionPayerAndMultiplierGreaterThanOne_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(multiplier: 1.2), Mock.Requests.Payer(multiplier: .2) };
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers, distribution: ExpenseDistribution.Proportional) };
            var patch = new JsonPatchDocument<RegisterExpense>();
            patch.Replace(x => x.Items, items);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _expenses.Expense = Mock.Models.DetailedExpense();
            _roommates.Roommate = Mock.Models.Roommate(id: _expenses.Expense.Payee.Id);

            // act
            var result = controller.Patch(_expenses.Expense.Id, patch);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Patch_DetailedExpenseWithProportionalDistributionAndMultiplierSumGreaterThanOne_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(multiplier: .5), Mock.Requests.Payer(multiplier: .9) };
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers, distribution: ExpenseDistribution.Proportional) };
            var patch = new JsonPatchDocument<RegisterExpense>();
            patch.Replace(x => x.Items, items);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _expenses.Expense = Mock.Models.DetailedExpense();
            _roommates.Roommate = Mock.Models.Roommate(id: _expenses.Expense.Payee.Id);

            // act
            var result = controller.Patch(_expenses.Expense.Id, patch);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Patch_DetailedExpenseWithProportionalDistributionAndMultiplierSumLowerThanOne_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(multiplier: .5), Mock.Requests.Payer(multiplier: .4) };
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers, distribution: ExpenseDistribution.Proportional) };
            var patch = new JsonPatchDocument<RegisterExpense>();
            patch.Replace(x => x.Items, items);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            _expenses.Expense = Mock.Models.DetailedExpense();
            _roommates.Roommate = Mock.Models.Roommate(id: _expenses.Expense.Payee.Id);

            // act
            var result = controller.Patch(_expenses.Expense.Id, patch);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payers"));
        }

        [Fact]
        public void Patch_DetailedExpenseWithPayments_ProducesBadRequest()
        {
            // arrange
            var controller = new ExpensesController(_channel, _expenses, _roommates);
            var payers = new[] { Mock.Requests.Payer(), Mock.Requests.Payer() };
            var items = new[] { Mock.Requests.ExpenseItem(payers: payers) };
            var patch = new JsonPatchDocument<RegisterExpense>();
            patch.Replace(x => x.Items, items);
            _roommates.Roommates = payers.Select(x => Mock.Models.Roommate(id: x.Id)).ToList();
            var payeeId = "payee-identifier";
            _roommates.Roommate = Mock.Models.Roommate(id: payeeId);
            _expenses.Expense = Mock.Models.DetailedExpense(payments: new[] { Mock.Models.PaymentSummary() });

            // act
            var result = controller.Patch(_expenses.Expense.Id, patch);

            // assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequest.Value);
            Assert.True(errors.ContainsKey("Payments"));
        }
        #endregion

        #region MemberData
        public static IEnumerable<object[]> EvenDistributions
        {
            get
            {
                return new[]
                {
                    new object[] { 120, new[] { 60M, 60M } },
                    new object[] { 3000, new[] { 1000M, 1000M, 1000M } },
                    new object[] { 5, new[] { 2.5M, 2.5M } }
                };
            }
        }

        public static IEnumerable<object[]> CustomDistributions
        {
            get
            {
                return new[]
                {
                    new object[] { 120,  new[] { 30M, 60M, 20M, 5M, 5M } },
                    new object[] { 3000, new[] { 1200M, 700M, 1100M } },
                    new object[] { 5,    new[] { 0.5M, 2.5M, 2M } }
                };
            }
        }

        public static IEnumerable<object[]> ProportionalDistributions
        {
            get
            {
                return new[]
                {
                    new object[] { 120,  new[] { .3, .45, .20, .05 }, new[] { 36M, 54M, 24M, 6M } },
                    new object[] { 3000, new[] { .15, .50, .35 },     new[] { 450M, 1500M, 1050M } },
                    new object[] { 5,    new[] { .7, .3 },            new[] { 3.5M, 1.5M } }
                };
            }
        }

        public static IEnumerable<object[]> BalancesEven
        {
            get
            {
                return new[]
                {
                    new object[] {
                        120M,
                        Mock.Models.Roommate(),
                        new[] { Mock.Models.Roommate(), Mock.Models.Roommate(), Mock.Models.Roommate() },
                        (-120M, new[] { 40M, 40M, 40M })
                    },
                    new object[] {
                        120M,
                        Mock.Models.Roommate(balance: 120M),
                        new[] { Mock.Models.Roommate(), Mock.Models.Roommate(), Mock.Models.Roommate() },
                        (0M, new[] { 40M, 40M, 40M })
                    },
                    new object[] {
                        120M,
                        Mock.Models.Roommate(),
                        new[] { Mock.Models.Roommate(balance: 40M), Mock.Models.Roommate(balance: -40M), Mock.Models.Roommate() },
                        (-120M, new[] { 80M, 0M, 40M })
                    }
                };
            }
        }

        public static IEnumerable<object[]> BalancesCustom
        {
            get
            {
                return new[]
                {
                    new object[] {
                        120M,
                        Mock.Models.Roommate(),
                        new[] { (10M, Mock.Models.Roommate()), (90M, Mock.Models.Roommate()), (20M, Mock.Models.Roommate()) },
                        (-120M, new[] { 10M, 90M, 20M })
                    },
                    new object[] {
                        110M,
                        Mock.Models.Roommate(balance: 120M),
                        new[] { (10M, Mock.Models.Roommate(balance: -50)), (90M, Mock.Models.Roommate()), (10M, Mock.Models.Roommate()) },
                        (10M, new[] { -40M, 90M, 10M })
                    },
                    new object[] {
                        120M,
                        Mock.Models.Roommate(),
                        new[] { (90M, Mock.Models.Roommate(balance: -90M)), (20M, Mock.Models.Roommate()), (7M, Mock.Models.Roommate()), (3M, Mock.Models.Roommate()) },
                        (-120M, new[] { 0M, 20M, 7M, 3M })
                    }
                };
            }
        }

        public static IEnumerable<object[]> BalancesProportional
        {
            get
            {
                return new[]
                {
                    new object[] {
                        120M,
                        Mock.Models.Roommate(),
                        new[] { (.2, Mock.Models.Roommate()), (.7, Mock.Models.Roommate()), (.1, Mock.Models.Roommate()) },
                        (-120M, new[] { 24M, 84M, 12M })
                    },
                    new object[] {
                        110M,
                        Mock.Models.Roommate(balance: 120M),
                        new[] { (.5, Mock.Models.Roommate(balance: -50)), (.2, Mock.Models.Roommate()), (.3, Mock.Models.Roommate()) },
                        (10M, new[] { 5M, 22M, 33M })
                    },
                    new object[] {
                        120M,
                        Mock.Models.Roommate(),
                        new[] { (.12, Mock.Models.Roommate(balance: -90M)), (.18, Mock.Models.Roommate()), (.4, Mock.Models.Roommate()), (.3, Mock.Models.Roommate()) },
                        (-120M, new[] { -75.6M, 21.6M, 48M, 36M })
                    }
                };
            }
        }
        #endregion
    }
}
