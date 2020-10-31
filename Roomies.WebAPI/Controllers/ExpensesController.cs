using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Mime;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Roomies.WebAPI.Extensions;
using Roomies.WebAPI.Models;
using Roomies.WebAPI.Repositories.Interfaces;
using Roomies.WebAPI.Requests;

namespace Roomies.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ApiConventionType(typeof(DefaultApiConventions))]
    public class ExpensesController : Controller
    {
        private readonly IExpensesRepository _expenses;
        private readonly IRoommatesRepository _roommates;
        private readonly ChannelWriter<IEnumerable<Autocomplete>> _channel;

        public ExpensesController(Channel<IEnumerable<Autocomplete>> channel, IExpensesRepository expenses, IRoommatesRepository roommates)
        {
            _channel = channel;
            _expenses = expenses;
            _roommates = roommates;
        }

        // GET: api/expenses
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<Expense>> Get() => Ok(_expenses.Get());

        // GET api/expenses/{id}
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<Expense> Get(string id)
        {
            var result = _expenses.Get(id);
            if (result != null) return Ok(result);

            return NotFound();
        }

        // POST api/expenses
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(Dictionary<string, string[]>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Expense>> Post([FromBody] RegisterExpense expense)
        {
            if (ModelState.IsValid)
            {
                List<Autocomplete> autocomplete;
                var result = RegisterExpense(expense, out autocomplete);
                if (result != null)
                {
                    await _channel.WriteAsync(autocomplete);
                    return CreatedAtAction(nameof(Post), new { id = result.Id }, result);
                }
            }
            
            return BadRequest(ModelState);
        }

        //// PUT api/expenses/{id}
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody] string value)
        //{
        //}

        // DELETE api/expenses/{id}
        [HttpDelete("{id}")]
        public ActionResult Delete(string id)
        {
            var expense = _expenses.Get(id);
            if (expense == null) return NotFound();

            IEnumerable<Payer> payers;
            if (expense is SimpleExpense simple)
                payers = simple.Payers;
            else if (expense is DetailedExpense detailed)
                payers = detailed.Items.SelectMany(x => x.Payers);
            else throw new ApplicationException("Something wrong has happened. An unpredicted condition has appeared.");

            Func<Payer, Payer> invertAmount = (payer) => { payer.Amount *= -1; return payer; };
            payers = payers.Select(invertAmount).ToList();
            UpdateBalances(payers, expense.Payee, -expense.Total);
            var isRemoved = _expenses.Remove(expense);

            if (isRemoved)
                return NoContent();
            else
            {
                // rollback balances
                payers = payers.Select(invertAmount).ToList();
                UpdateBalances(payers, expense.Payee, expense.Total);

                throw new ApplicationException("Something wrong has happened. The expense could not be deleted.");
            }
        }

        // GET: api/expenses/{expenseId}/items
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpGet("{expenseId}/Items")]
        public ActionResult<IEnumerable<ExpenseItem>> GetItems(string expenseId)
        {
            var result = _expenses.GetItems(expenseId);
            if (result != null)
                return Ok(result);

            return NotFound();
        }

        // GET api/expenses/{expenseId}/items/{id}
        [HttpGet("{expenseId}/Items/{itemId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<ExpenseItem> GetItem(string expenseId, int itemId)
        {
            var result = _expenses.GetItem(expenseId, itemId);
            if (result != null)
                return Ok(result);

            return NotFound();
        }

        private Expense RegisterExpense(RegisterExpense expense, out List<Autocomplete> autocomplete)
        {
            autocomplete = new List<Autocomplete>();
            autocomplete.Add(new Autocomplete { Text = expense.BusinessName, Type = AutocompleteType.BusinessName });
            if (expense.Items?.Any() == true)
                return RegisterDetailedExpense(expense, autocomplete);
            else
                return RegisterSimpleExpense(expense, autocomplete);
        }

        private Expense RegisterSimpleExpense(RegisterExpense simpleExpense, List<Autocomplete> autocomplete)
        {
            var entity = ValidateSimpleExpense(simpleExpense);
            if (!ModelState.IsValid) return null;

            if (simpleExpense.Description.Length < 31)
                autocomplete.Add(new Autocomplete { Text = simpleExpense.Description, Type = AutocompleteType.ItemName });

            var result = _expenses.Add(entity);
            if (result != null)
                UpdateBalances(entity.Payers, entity.Payee, entity.Total);

            return result;
        }

        private Expense RegisterDetailedExpense(RegisterExpense detailedExpense, List<Autocomplete> autocomplete)
        {
            var entity = ValidateDetailedExpense(detailedExpense);
            if (!ModelState.IsValid) return null;

            foreach (var item in entity.Items)
                autocomplete.Add(new Autocomplete { Text = item.Name, Type = AutocompleteType.ItemName });

            var result = _expenses.Add(entity);
            if (result != null)
                UpdateBalances(entity.Items.SelectMany(x => x.Payers), entity.Payee, entity.Total);

            return result;
        }

        private SimpleExpense ValidateSimpleExpense(RegisterExpense simpleExpense)
        {
            if (simpleExpense.Payers?.Any() != true)
            {
                ModelState.AddModelError("Payers", "When registering a Simple Expense, you must specify at least one Payer.");
                return null;
            }

            if (simpleExpense.Distribution == null)
            {
                ModelState.AddModelError("Distribution", "When registering a Simple Expense, you must specify the type of distribution.");
                return null;
            }

            #region Validate Payers & Payee
            var roommate = _roommates.Get(simpleExpense.PayeeId);
            if (roommate == null)
                ModelState.AddModelError("PayeeId", "The specified PayeeId is not valid or does not represent a registered Roommate.");
            var payee = new Payee { Id = roommate.Id, Name = roommate.Name };

            // TODO: validate duplications before calling the database
            var roommates = _roommates.Get(simpleExpense.Payers.Select(x => x.Id));
            Validate(simpleExpense.Payers, roommates, payee);
            if (!ModelState.IsValid) return null;
            #endregion

            #region Validate Distribution
            Validate(simpleExpense.Distribution.Value, simpleExpense.Payers);
            if (!ModelState.IsValid) return null;
            #endregion

            #region Parse Entity
            var entity = (SimpleExpense)simpleExpense;
            entity.Payee = payee;
            entity.Payers = simpleExpense.Payers.Select(x => new Payer
            {
                Id = x.Id,
                Amount = simpleExpense.Distribution.Value.GetAmount(simpleExpense, x),
                Name = roommates.Single(p => p.Id == x.Id).Name
            }).ToList();
            #endregion

            #region Validate Totals
            var payersTotal = entity.Payers.Sum(x => x.Amount);
            if (payersTotal != entity.Total)
            {
                ModelState.AddModelError("Total", "The total amount for this expense and the total amount by payers' distribution differ.");
                ModelState.AddModelError("Total", $"Invoice total: {entity.Total}");
                ModelState.AddModelError("Payers", "The total amount for this expense and the total amount by payers' distribution differ.");
                ModelState.AddModelError("Payers", $"Payer's total: {payersTotal}.");
                return null;
            }
            #endregion

            return entity;
        }

        private DetailedExpense ValidateDetailedExpense(RegisterExpense detailedExpense)
        {
            if (detailedExpense.Items?.Any() != true)
            {
                ModelState.AddModelError("Payers", "When registering a Detailed Expense, you must specify at least one Item.");
                return null;
            }

            #region Validate Payee & Payers
            var roommate = _roommates.Get(detailedExpense.PayeeId);
            if (roommate == null)
                ModelState.AddModelError("PayeeId", "The specified PayeeId is not valid or does not represent a registered Roommate.");
            var payee = new Payee { Id = roommate.Id, Name = roommate.Name };

            // TODO: validate duplications before calling the database
            var payers = detailedExpense.Items.SelectMany(i => i.Payers).Distinct(new PayerEqualityComparer()).ToList();
            var ids = payers.Select(p => p.Id).ToList();
            var roommates = _roommates.Get(ids);

            Validate(payers, roommates, payee);
            if (!ModelState.IsValid) return null;
            #endregion

            #region Validate Items
            foreach (var item in detailedExpense.Items)
            {
                Validate(item.Distribution, item.Payers);
                if (!ModelState.IsValid) return null;
            }
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
                    Amount = i.Distribution.GetAmount(i, p)
                }).ToList();
                return item;
            }).ToList();
            #endregion

            #region Validate Totals
            var itemsTotal = entity.Items.Sum(x => x.Total);
            if (itemsTotal != entity.Total)
            {
                ModelState.AddModelError("Items", "The total amount for this expense and the total amount by items differ.");
                ModelState.AddModelError("Items", $"Item's total: {itemsTotal}.");
                ModelState.AddModelError("Total", "The total amount for this expense and the total amount by items differ.");
                ModelState.AddModelError("Total", $"Invoice total: {entity.Total}");
                return null;
            }
            var payersTotal = entity.Items.Sum(i => i.Payers.Sum(p => p.Amount));
            if (payersTotal != entity.Total)
            {
                ModelState.AddModelError("Total", "The total amount for this expense and the total amount by payers' distribution differ.");
                ModelState.AddModelError("Total", $"Invoice total: {entity.Total}");
                ModelState.AddModelError("Payers", "The total amount for this expense and the total amount by payers' distribution differ.");
                ModelState.AddModelError("Payers", $"Payer's total: {payersTotal}.");
                return null;
            }
            #endregion

            return entity;
        }

        private void Validate(IEnumerable<RegisterExpensePayer> payers, IEnumerable<Roommate> roommates, Payee payee)
        {
            if (!payers.Any() || payers.Count() != roommates.Count())
                ModelState.AddModelError("Payers", "At least one Payer is invalid, does not represent a registered Roommate, or is duplicated.");

            if (payers.Count() == 1 && payers.First().Id == payee.Id)
            {
                ModelState.AddModelError("PayeeId", "At this moment, self expenses are not supported. Please consider other alternatives.");
                ModelState.AddModelError("Payers", "At this moment, self expenses are not supported. Please consider other alternatives.");
            }
        }

        private void Validate(ExpenseDistribution distribution, IEnumerable<RegisterExpensePayer> payers)
        {
            var hasInvalidPayer = payers.Any(x => x.Amount != null && x.Multiplier != null);
            if (hasInvalidPayer)
                ModelState.AddModelError("Payers", "An Expense cannot be proportional and custom at the same time. Amount and Multiplier cannot be filled at the same time. Please, select only one.");

            var hasInvalidAmount = payers.Any(x => x.Amount == null || x.Amount <= 0);
            var hasInvalidMultiplier = payers.Any(x => x.Multiplier == null || x.Multiplier > 1 || x.Multiplier <= 0) || payers.Sum(x => (float)x.Multiplier) != 1;
            if (distribution == ExpenseDistribution.Custom && hasInvalidAmount)
                ModelState.AddModelError("Payers", "An Expense with custom distribution must specify payers' custom amount and it must be greater than 0.");
            else if (distribution == ExpenseDistribution.Proportional && hasInvalidMultiplier)
                ModelState.AddModelError("Payers", "An Expense with proportional distribution must specify payers' multiplier and it must be between 0 and 1.");
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


        private class PayerEqualityComparer : IEqualityComparer<RegisterExpensePayer>
        {
            public bool Equals([AllowNull] RegisterExpensePayer x, [AllowNull] RegisterExpensePayer y) => x?.Id == y?.Id;

            public int GetHashCode([DisallowNull] RegisterExpensePayer obj) => obj.Id.GetHashCode();
        }
    }
}
