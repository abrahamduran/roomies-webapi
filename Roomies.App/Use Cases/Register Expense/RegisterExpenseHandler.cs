using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Roomies.App.Extensions;
using Roomies.App.Models;
using Roomies.App.Persistence.Interfaces;

namespace Roomies.App.UseCases.RegisterExpense
{
    public class RegisterExpenseHandler
    {
        private const int ROUNDED_PLACES = 3;
        private const decimal MAX_OFFSET_VALUE = 0.1M;

        private readonly IExpensesRepository _expenses;
        private readonly IRoommatesRepository _roommates;

        public RegisterExpenseHandler(IExpensesRepository expenses, IRoommatesRepository roommates)
        {
            _expenses = expenses;
            _roommates = roommates;
        }

        public Response Execute(RegisterExpenseRequest expense, out List<Autocomplete> autocomplete)
        {
            autocomplete = new List<Autocomplete>();
            autocomplete.Add(new Autocomplete { Text = expense.BusinessName, Type = AutocompleteType.BusinessName });

            expense.Tags = expense.Tags?.Select(x => x.ToLower()).ToArray();
            if (expense.Tags?.Any(x => x.Contains(' ') || x.Contains('_')) == true)
                throw new UseCaseException("Tags", "Tags cannot contain whitespaces. Use only dashes (-) instead.");

            if (expense.Items?.Any() == true)
                return toResponse(RegisterDetailedExpense(expense, autocomplete));
            else
                return toResponse(RegisterSimpleExpense(expense, autocomplete));
        }

        private Expense RegisterSimpleExpense(RegisterExpenseRequest simpleExpense, List<Autocomplete> autocomplete)
        {
            var entity = ValidateSimpleExpense(simpleExpense);

            if (simpleExpense.Description?.Length < 31)
                autocomplete.Add(new Autocomplete { Text = simpleExpense.Description, Type = AutocompleteType.ItemName });

            var result = _expenses.Add(entity);
            if (result != null)
                UpdateBalances(entity.Payers, entity.Payee, entity.Total);

            return result;
        }

        private Expense RegisterDetailedExpense(RegisterExpenseRequest detailedExpense, List<Autocomplete> autocomplete)
        {
            var entity = ValidateDetailedExpense(detailedExpense);

            foreach (var item in entity.Items)
                autocomplete.Add(new Autocomplete { Text = item.Name, Type = AutocompleteType.ItemName });

            var result = _expenses.Add(entity);
            if (result != null)
                UpdateBalances(entity.Items.SelectMany(x => x.Payers), entity.Payee, entity.Total);

            return result;
        }

        private Expense ValidateExpense(RegisterExpenseRequest expense)
        {
            expense.Tags = expense.Tags?.Select(x => x.ToLower()).ToArray();
            if (expense.Tags?.Any(x => x.Contains(' ') || x.Contains('_')) == true)
                throw new UseCaseException("Tags", "Tags cannot contain whitespaces. Use only dashes (-) instead.");

            if (expense.Items?.Any() == true)
                return ValidateDetailedExpense(expense);
            else
                return ValidateSimpleExpense(expense);
        }

        private SimpleExpense ValidateSimpleExpense(RegisterExpenseRequest simpleExpense)
        {
            if (simpleExpense.Payers?.Any() != true)
                throw new UseCaseException("Payers", "When registering a Simple Expense, you must specify at least one Payer.");

            if (simpleExpense.Distribution == null && simpleExpense.Payers?.Count() != 1)
                throw new UseCaseException("Distribution", "When registering a Simple Expense, you must specify the type of distribution.");
            else if (simpleExpense.Distribution == null && simpleExpense.Payers?.Count() == 1)
                simpleExpense.Distribution = ExpenseDistribution.Even;

            #region Validate Payers & Payee
            var roommate = _roommates.Get(simpleExpense.PayeeId);
            if (roommate == null)
                throw new UseCaseException("PayeeId", "The specified PayeeId is not valid or does not represent a registered Roommate.");

            var payee = new Payee { Id = roommate.Id, Name = roommate.Name };

            // TODO: validate duplications before calling the database
            var roommates = _roommates.Get(simpleExpense.Payers.Select(x => x.Id));
            Validate(simpleExpense.Payers, roommates, payee);
            #endregion

            #region Validate Distribution
            Validate(simpleExpense.Distribution.Value, simpleExpense.Payers);
            #endregion

            #region Parse Entity
            var entity = (SimpleExpense)simpleExpense;
            entity.Payee = payee;
            entity.Payers = simpleExpense.Payers.Select(x => new Payer
            {
                Id = x.Id,
                Amount = simpleExpense.Distribution.Value.GetAmount(simpleExpense, x).Rounded(ROUNDED_PLACES),
                Name = roommates.Single(p => p.Id == x.Id).Name
            }).ToList();
            #endregion

            #region Validate Totals
            var payersTotal = entity.Payers.Sum(x => x.Amount);
            if (payersTotal < entity.Total || payersTotal > (entity.Total + MAX_OFFSET_VALUE))
            {
                var ex = new UseCaseException();
                ex.AddError("Total", "The total amount for this expense and the total amount by payers' distribution differ.");
                ex.AddError("Total", $"Invoice total: {entity.Total}");
                ex.AddError("Payers", "The total amount for this expense and the total amount by payers' distribution differ.");
                ex.AddError("Payers", $"Payer's total: {payersTotal}.");
                throw ex;
            }
            #endregion

            return entity;
        }

        private DetailedExpense ValidateDetailedExpense(RegisterExpenseRequest detailedExpense)
        {
            if (detailedExpense.Items?.Any() != true)
                throw new UseCaseException("Payers", "When registering a Detailed Expense, you must specify at least one Item.");

            #region Validate Payee & Payers
            var roommate = _roommates.Get(detailedExpense.PayeeId);
            if (roommate == null)
                throw new UseCaseException("PayeeId", "The specified PayeeId is not valid or does not represent a registered Roommate.");

            var payee = new Payee { Id = roommate.Id, Name = roommate.Name };

            // TODO: validate duplications before calling the database
            var payers = detailedExpense.Items.SelectMany(i => i.Payers).Distinct().ToList();
            var ids = payers.Select(p => p.Id).ToList();
            var roommates = _roommates.Get(ids);

            Validate(payers, roommates, payee);
            #endregion

            #region Validate Items
            foreach (var item in detailedExpense.Items)
                Validate(item.Distribution, item.Payers);
            #endregion

            #region Parse Entity
            var entity = (DetailedExpense)detailedExpense;
            entity.Payee = payee;

            var itemId = 1;
            entity.Items = detailedExpense.Items.Select(i =>
            {
                var item = (ExpenseItem)i;
                item.Id = itemId++;
                item.Payers = i.Payers.Select(p => new Payer
                {
                    Id = p.Id,
                    Name = roommates.Single(x => x.Id == p.Id).Name,
                    Amount = i.Distribution.GetAmount(i, p).Rounded(3)
                }).ToList();
                return item;
            }).ToList();
            #endregion

            #region Validate Totals
            var itemsTotal = entity.Items.Sum(x => x.Total);
            if (itemsTotal != entity.Total)
            {
                var ex = new UseCaseException();
                ex.AddError("Items", "The total amount for this expense and the total amount by items differ.");
                ex.AddError("Items", $"Item's total: {itemsTotal}.");
                ex.AddError("Total", "The total amount for this expense and the total amount by items differ.");
                ex.AddError("Total", $"Invoice total: {entity.Total}");
                throw ex;
            }
            var payersTotal = entity.Items.Sum(i => i.Payers.Sum(p => p.Amount));
            if (payersTotal < entity.Total || payersTotal > (entity.Total + MAX_OFFSET_VALUE))
            {
                var ex = new UseCaseException();
                ex.AddError("Total", "The total amount for this expense and the total amount by payers' distribution differ.");
                ex.AddError("Total", $"Invoice total: {entity.Total}");
                ex.AddError("Payers", "The total amount for this expense and the total amount by payers' distribution differ.");
                ex.AddError("Payers", $"Payer's total: {payersTotal}.");
                throw ex;
            }
            #endregion

            return entity;
        }

        private void Validate(IEnumerable<RegisterExpenseRequest.Payer> payers, IEnumerable<Roommate> roommates, Payee payee)
        {
            var ex = new UseCaseException();
            if (!payers.Any() || payers.Count() != roommates.Count())
                ex.AddError("Payers", "At least one Payer is invalid, does not represent a registered Roommate, or is duplicated.");

            if (payers.Count() == 1 && payers.First().Id == payee.Id)
            {
                ex.AddError("PayeeId", "At this moment, self expenses are not supported. Please consider other alternatives.");
                ex.AddError("Payers", "At this moment, self expenses are not supported. Please consider other alternatives.");
            }

            if (ex.Errors.Any()) throw ex;
        }

        private void Validate(ExpenseDistribution distribution, IEnumerable<RegisterExpenseRequest.Payer> payers)
        {
            var ex = new UseCaseException();
            var hasInvalidPayer = payers.Any(x => x.Amount != null && x.Multiplier != null);
            if (hasInvalidPayer)
                ex.AddError("Payers", "An Expense cannot be proportional and custom at the same time. Amount and Multiplier cannot be filled at the same time. Please, select only one.");

            var hasInvalidAmount = payers.Any(x => x.Amount == null || x.Amount <= 0);
            var hasInvalidMultiplier = payers.Any(x => x.Multiplier == null || x.Multiplier > 1 || x.Multiplier <= 0) || payers.Sum(x => (float)x.Multiplier) != 1;
            if (distribution == ExpenseDistribution.Custom && hasInvalidAmount)
                ex.AddError("Payers", "An Expense with custom distribution must specify payers' custom amount and it must be greater than 0.");
            else if (distribution == ExpenseDistribution.Proportional && hasInvalidMultiplier)
                ex.AddError("Payers", "An Expense with proportional distribution must specify payers' multiplier and it must be between 0 and 1.");

            if (ex.Errors.Any()) throw ex;
        }

        private void UpdateBalances(IEnumerable<Payer> payers, Payee payee, decimal total)
        {
            decimal payeeAmount = 0;

            foreach (var payer in payers)
            {
                if (payer.Id == payee.Id)
                {
                    payeeAmount += payer.Amount;
                    continue;
                }

                _roommates.UpdateBalance(payer.Id, payer.Amount);
            }

            if (payeeAmount > 0)
                _roommates.UpdateBalance(payee.Id, -total + payeeAmount);
            else
                _roommates.UpdateBalance(payee.Id, -total);
        }

        private Response toResponse(Expense expense) => Response.ForExpense(expense, false);
    }
}
