using System;
using System.Collections.Generic;
using System.Linq;
using Roomies.Tests.Mocks;
using Roomies.App.Models;
using Roomies.WebAPI.Requests;
using Xunit;

namespace Roomies.Tests.UnitTests
{
    public class ModelMappingTests
    {
        [Fact]
        public void SimpleExpenseWithEvenDistribution_MapsTo_RegisterExpense()
        {
            // arrange
            var expense = Mock.Models.SimpleExpense();

            // act
            var register = RegisterExpense.From(expense);

            // assert
            Assert.Equal(expense.BusinessName, register.BusinessName);
            Assert.Equal(expense.Date, register.Date);
            Assert.Equal(expense.Description, register.Description);
            Assert.Equal(expense.Distribution, register.Distribution);
            Assert.Equal(expense.Payee.Id, register.PayeeId);
            Assert.Equal(expense.Total, register.Total);
            Assert.Equal(expense.Payers.Select(x => x.Id).ToList(), register.Payers.Select(x => x.Id).ToList());
            Assert.Null(register.Items);
            foreach (var payer in register.Payers)
            {
                Assert.Null(payer.Amount);
                Assert.Null(payer.Multiplier);
            }
        }

        [Theory, MemberData(nameof(ProportionalAmounts))]
        public void SimpleExpenseWithProportionalDistribution_MapsTo_RegisterExpense(decimal[] amounts, decimal total, double[] expectedMultipliers)
        {
            // arrange
            var payers = amounts.Select(x => Mock.Models.Payer(amount: x)).ToArray();
            var expense = Mock.Models.SimpleExpense(total: total, payers: payers, distribution: ExpenseDistribution.Proportional);  

            // act
            var register = RegisterExpense.From(expense);

            // assert
            Assert.Equal(expense.BusinessName, register.BusinessName);
            Assert.Equal(expense.Date, register.Date);
            Assert.Equal(expense.Description, register.Description);
            Assert.Equal(expense.Distribution, register.Distribution);
            Assert.Equal(expense.Payee.Id, register.PayeeId);
            Assert.Equal(expense.Total, register.Total);
            Assert.Equal(expense.Payers.Select(x => x.Id).ToList(), register.Payers.Select(x => x.Id).ToList());
            Assert.Equal(expectedMultipliers, register.Payers.Select(x => x.Multiplier.Value).ToArray());
            Assert.Null(register.Items);
            foreach (var payer in register.Payers)
                Assert.Null(payer.Amount);
        }

        [Theory, MemberData(nameof(CustomAmounts))]
        public void SimpleExpenseWithCustomDistribution_MapsTo_RegisterExpense(decimal[] amounts, decimal total)
        {
            // arrange
            var payers = amounts.Select(x => Mock.Models.Payer(amount: x)).ToArray();
            var expense = Mock.Models.SimpleExpense(total: total, payers: payers, distribution: ExpenseDistribution.Custom);

            // act
            var register = RegisterExpense.From(expense);

            // assert
            Assert.Equal(expense.BusinessName, register.BusinessName);
            Assert.Equal(expense.Date, register.Date);
            Assert.Equal(expense.Description, register.Description);
            Assert.Equal(expense.Distribution, register.Distribution);
            Assert.Equal(expense.Payee.Id, register.PayeeId);
            Assert.Equal(expense.Total, register.Total);
            Assert.Equal(expense.Payers.Select(x => x.Id).ToList(), register.Payers.Select(x => x.Id).ToList());
            Assert.Equal(amounts, register.Payers.Select(x => x.Amount.Value).ToArray());
            Assert.Null(register.Items);
            foreach (var payer in register.Payers)
                Assert.Null(payer.Multiplier);
        }

        [Fact]
        public void DetailedExpenseWithEvenDistribution_MapsTo_RegisterExpense()
        {
            // arrange
            var expense = Mock.Models.DetailedExpense();

            // act
            var register = RegisterExpense.From(expense);

            // assert
            Assert.Equal(expense.BusinessName, register.BusinessName);
            Assert.Equal(expense.Date, register.Date);
            Assert.Equal(expense.Description, register.Description);
            Assert.Equal(expense.Payee.Id, register.PayeeId);
            Assert.Equal(expense.Total, register.Total);
            Assert.Equal(expense.Items.Count(), register.Items.Count());
            for (int i = 0; i < expense.Items.Count(); i++)
            {
                (var left, var right) = (expense.Items.ElementAt(i), register.Items.ElementAt(i));
                Assert.Equal(left.Distribution, right.Distribution);
                Assert.Equal(left.Name, right.Name);
                Assert.Equal(left.Price, right.Price);
                Assert.Equal(left.Quantity, right.Quantity);
                Assert.Equal(left.Total, right.Total);
                Assert.Equal(left.Payers.Select(x => x.Id).ToList(), right.Payers.Select(x => x.Id).ToList());
                foreach (var payer in right.Payers)
                {
                    Assert.Null(payer.Amount);
                    Assert.Null(payer.Multiplier);
                }
            }
            Assert.Null(register.Payers);
            Assert.Null(register.Distribution);
        }

        [Theory, MemberData(nameof(ProportionalAmounts))]
        public void DetailedExpenseWithProportionalDistribution_MapsTo_RegisterExpense(decimal[] amounts, decimal total, double[] expectedMultipliers)
        {
            // arrange
            var payers = amounts.Select(x => Mock.Models.Payer(amount: x)).ToArray();
            var items = new[] { Mock.Models.ExpenseItem(price: total, payers: payers, distribution: ExpenseDistribution.Proportional) };
            var expense = Mock.Models.DetailedExpense(total: total, items: items);

            // act
            var register = RegisterExpense.From(expense);

            // assert
            Assert.Equal(expense.BusinessName, register.BusinessName);
            Assert.Equal(expense.Date, register.Date);
            Assert.Equal(expense.Description, register.Description);
            Assert.Equal(expense.Payee.Id, register.PayeeId);
            Assert.Equal(expense.Total, register.Total);
            Assert.Equal(expense.Items.Count(), register.Items.Count());
            for (int i = 0; i < expense.Items.Count(); i++)
            {
                (var left, var right) = (expense.Items.ElementAt(i), register.Items.ElementAt(i));
                Assert.Equal(left.Distribution, right.Distribution);
                Assert.Equal(left.Name, right.Name);
                Assert.Equal(left.Price, right.Price);
                Assert.Equal(left.Quantity, right.Quantity);
                Assert.Equal(left.Total, right.Total);
                Assert.Equal(left.Payers.Select(x => x.Id).ToList(), right.Payers.Select(x => x.Id).ToList());
                Assert.Equal(expectedMultipliers, right.Payers.Select(x => x.Multiplier.Value).ToArray());
                foreach (var payer in right.Payers)
                    Assert.Null(payer.Amount);
            }
            Assert.Null(register.Payers);
            Assert.Null(register.Distribution);
        }

        [Theory, MemberData(nameof(CustomAmounts))]
        public void DetailedExpenseWithCustomDistribution_MapsTo_RegisterExpense(decimal[] amounts, decimal total)
        {
            // arrange
            var payers = amounts.Select(x => Mock.Models.Payer(amount: x)).ToArray();
            var items = new[] { Mock.Models.ExpenseItem(price: total, payers: payers, distribution: ExpenseDistribution.Custom) };
            var expense = Mock.Models.DetailedExpense(total: total, items: items);

            // act
            var register = RegisterExpense.From(expense);

            // assert
            Assert.Equal(expense.BusinessName, register.BusinessName);
            Assert.Equal(expense.Date, register.Date);
            Assert.Equal(expense.Description, register.Description);
            Assert.Equal(expense.Payee.Id, register.PayeeId);
            Assert.Equal(expense.Total, register.Total);
            Assert.Equal(expense.Items.Count(), register.Items.Count());
            for (int i = 0; i < expense.Items.Count(); i++)
            {
                (var left, var right) = (expense.Items.ElementAt(i), register.Items.ElementAt(i));
                Assert.Equal(left.Distribution, right.Distribution);
                Assert.Equal(left.Name, right.Name);
                Assert.Equal(left.Price, right.Price);
                Assert.Equal(left.Quantity, right.Quantity);
                Assert.Equal(left.Total, right.Total);
                Assert.Equal(left.Payers.Select(x => x.Id).ToList(), right.Payers.Select(x => x.Id).ToList());
                Assert.Equal(amounts, right.Payers.Select(x => x.Amount.Value).ToArray());
                foreach (var payer in right.Payers)
                    Assert.Null(payer.Multiplier);
            }
            Assert.Null(register.Payers);
            Assert.Null(register.Distribution);
        }

        public static IEnumerable<object[]> ProportionalAmounts
        {
            get
            {
                return new[]
                {
                    new object[] {
                        new[] { 60M, 40M },
                        100M,
                        new[] { 0.6, 0.4 }
                    },
                    new object[] {

                        new[] { 414.6M, 621.9M, 207.3M, 829.2M },
                        2073,
                        new[] { 0.2, 0.3, 0.1, 0.4 }
                    },
                    new object[] {

                        new[] { 240.772M, 481.544M, 120.386M, 361.158M },
                        1203.86,
                        new[] { 0.2, 0.4, 0.1, 0.3 }
                    }
                };
            }
        }

        public static IEnumerable<object[]> CustomAmounts
        {
            get
            {
                return new[]
                {
                    new object[] {
                        new[] { 60M, 40M },
                        100M
                    },
                    new object[] {

                        new[] { 414.6M, 621.9M, 207.3M, 829.2M },
                        2073
                    },
                    new object[] {

                        new[] { 240.772M, 481.544M, 120.386M, 361.158M },
                        1203.86
                    }
                };
            }
        }
    }
}
