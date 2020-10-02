using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly RoommatesRespositoryMock _roommates;
        private readonly ChannelMock<IEnumerable<Autocomplete>> _channel;

        public ExpensesControllerTests()
        {
            _expenses = new ExpensesRepositoryMock();
            _roommates = new RoommatesRespositoryMock();
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
        public async Task Post_RegisterSimpleExpenseWithIncorrectTotal_ReturnsBadRequest()
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
        public async Task Post_RegisterSimpleExpenseWithNoPayers_ReturnsBadRequests()
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
        public async Task Post_RegisterSimpleExpenseWithSelfExpenses_ReturnsBadRequests()
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
        public async Task Post_RegisterSimpleExpense_PayerWithAmountAndMultiplier_ReturnsBadRequests()
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
        public async Task Post_RegisterSimpleExpense_CustomDistributionWithNullAmount_ReturnsBadRequest()
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
        public async Task Post_RegisterSimpleExpense_CustomDistributionWithNegativeAmount_ReturnsBadRequest()
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
        public async Task Post_RegisterSimpleExpense_ProportionalDistributionWithNullAmount_ReturnsBadRequest()
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
        public async Task Post_RegisterSimpleExpense_ProportionalDistributionWithNegativeAmount_ReturnsBadRequest()
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
        public async Task Post_RegisterSimpleExpense_ProportionalDistributionPayerWithMultiplierGreaterThanOne_ReturnsBadRequest()
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
        public async Task Post_RegisterSimpleExpense_ProportionalDistributionWithMultiplierSumGreaterThanOne_ReturnsBadRequest()
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
        public async Task Post_RegisterSimpleExpense_ProportionalDistributionWithMultiplierSumLowerThanOne_ReturnsBadRequest()
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
        public async Task Post_RegisterDetailedExpenseWithIncorrectTotal_ReturnsBadRequest()
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
            Assert.True(errors.ContainsKey("Total"));
            Assert.True(errors.ContainsKey("Payers"));
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
        public async Task Post_RegisterDetailedExpenseWithNoPayers_ReturnsBadRequests()
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
        public async Task Post_RegisterDetailedExpenseWithSelfExpenses_ReturnsBadRequests()
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
        public async Task Post_RegisterDetailedExpense_PayerWithAmountAndMultiplier_ReturnsBadRequests()
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
        public async Task Post_RegisterDetailedExpense_CustomDistributionWithNullAmount_ReturnsBadRequest()
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
        public async Task Post_RegisterDetailedExpense_CustomDistributionWithNegativeAmount_ReturnsBadRequest()
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
        public async Task Post_RegisterDetailedExpense_ProportionalDistributionWithNullAmount_ReturnsBadRequest()
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
        public async Task Post_RegisterDetailedExpense_ProportionalDistributionWithNegativeAmount_ReturnsBadRequest()
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
        public async Task Post_RegisterDetailedExpense_ProportionalDistributionPayerWithMultiplierGreaterThanOne_ReturnsBadRequest()
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
        public async Task Post_RegisterDetailedExpense_ProportionalDistributionWithMultiplierSumGreaterThanOne_ReturnsBadRequest()
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
        public async Task Post_RegisterDetailedExpense_ProportionalDistributionWithMultiplierSumLowerThanOne_ReturnsBadRequest()
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
