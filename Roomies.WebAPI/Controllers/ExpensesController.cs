using System.Collections.Generic;
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
            var result = _expenses.GetById(id);
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
                #region Validate Payee
                var roommate = _roommates.GetById(expense.PayeeId);
                if (roommate == null)
                {
                    ModelState.AddModelError("PayeeId", "The specified PayeeId is not valid or does not represent a registered Roommate.");
                    return BadRequest(ModelState);
                }
                var payee = new Payee { Id = roommate.Id, Name = roommate.Name };
                #endregion

                List<Autocomplete> autocomplete;
                var result = RegisterExpense(expense, payee, out autocomplete);
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

        //// DELETE api/expenses/{id}
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}

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

        private Expense RegisterExpense(RegisterExpense expense, Payee payee, out List<Autocomplete> autocomplete)
        {
            autocomplete = new List<Autocomplete>();
            autocomplete.Add(new Autocomplete { Text = expense.BusinessName, Type = AutocompleteType.BusinessName });
            if (expense.Items?.Any() == true)
                return RegisterDetailedExpense(expense, payee, autocomplete);
            else
                return RegisterSimpleExpense(expense, payee, autocomplete);
        }

        private Expense RegisterSimpleExpense(RegisterExpense simpleExpense, Payee payee, List<Autocomplete> autocomplete)
        {
            var entity = (SimpleExpense)simpleExpense;
            entity.Payee = payee;

            #region Validations
            if (simpleExpense.Payers?.Any() != true)
            {
                ModelState.AddModelError("Payers", "When registering a Simple Expense, you must specify at least one Payer.");
                return null;
            }

            #region Validate Payers
            // TODO: validate duplications before calling the database
            var payers = _roommates.GetByIds(simpleExpense.Payers.Select(x => x.Id));
            if (payers.Count() != simpleExpense.Payers.Count())
            {
                ModelState.AddModelError("Payers", "At least one Payer is invalid, does not represent a registered Roommate, or is duplicated.");
                return null;
            }
            else if (payers.Count() == 1 && payers.First().Id == payee.Id)
            {
                ModelState.AddModelError("Payers", "At this moment, self expenses are not supported. Please consider other alternatives.");
                return null;
            }
            var sum = simpleExpense.Payers.Sum(x => x.Amount * (decimal)x.Multiplier);
            if (sum > 0)
            {
                ModelState.AddModelError("Payers", "An Expense cannot be proportional and even at the same time. Amount and Multiplier cannot be filled at the same time. Please, select only one.");
                return null;
            }
            entity.Payers = simpleExpense.Payers.Select(x => new Payer
            {
                Id = x.Id,
                Amount = simpleExpense.Distribution.GetAmount(simpleExpense, x),
                Name = payers.Single(p => p.Id == x.Id).Name
            }).ToList();
            var total = entity.Payers.Sum(x => x.Amount);
            if (total != simpleExpense.Total)
            {
                ModelState.AddModelError("Payers", "The total amount for this expense and the total amount by payers' distribution differ.");
                return null;
            }
            #endregion
            #endregion

            if (simpleExpense.Description.Length < 31)
                autocomplete.Add(new Autocomplete { Text = simpleExpense.Description, Type = AutocompleteType.ItemName });

            var result = _expenses.Add(entity);
            if (result != null)
                UpdateBalances(entity.Payers, entity.Payee, entity.Total);

            return result;
        }

        private Expense RegisterDetailedExpense(RegisterExpense detailedExpense, Payee payee, List<Autocomplete> autocomplete)
        {
            var entity = (DetailedExpense)detailedExpense;
            entity.Payee = payee;

            #region Validations
            if (detailedExpense.Items?.Any() != true)
            {
                ModelState.AddModelError("Payers", "When registering a Detailed Expense, you must specify at least one Item.");
                return null;
            }

            #region Validate Payers
            // TODO: validate duplications before calling the database
            var ids = detailedExpense.Items.SelectMany(i => i.Payers.Select(p => p.Id)).Distinct();
            var payers = _roommates.GetByIds(ids);
            if (payers.Count() != ids.Count())
            {
                ModelState.AddModelError("Payers", "At least one Payer is invalid or does not represent a registered Roommate, or is duplicated.");
                return null;
            }
            else if (payers.Count() == 1 && payers.First().Id == payee.Id)
            {
                ModelState.AddModelError("Payers", "At this moment, self expenses are not supported. Please consider other alternatives.");
                return null;
            }
            var sum = detailedExpense.Items.Sum(i => i.Payers.Sum(p => p.Amount * (decimal)p.Multiplier));
            if (sum > 0)
            {
                ModelState.AddModelError("Payers", "An Item cannot be proportional and even at the same time. Amount and Multiplier cannot be filled at the same time. Please, select only one.");
                return null;
            }
            var itemId = 1;
            entity.Items = detailedExpense.Items.Select(i =>
            {
                var item = (ExpenseItem)i;
                item.Id = itemId++;
                item.Payers = i.Payers.Select(p => new Payer
                {
                    Id = p.Id,
                    Name = payers.Single(x => x.Id == p.Id).Name,
                    Amount = i.Distribution.GetAmount(i, p)
                }).ToList();
                autocomplete.Add(new Autocomplete { Text = item.Name, Type = AutocompleteType.ItemName });
                return item;
            }).ToList();
            var itemsTotal = entity.Items.Sum(x => x.Total);
            if (itemsTotal != entity.Total)
            {
                ModelState.AddModelError("Items", "The total amount for this expense and the total amount by items differ.");
                return null;
            }
            var total = entity.Items.Sum(i => i.Payers.Sum(p => p.Amount));
            if (total != detailedExpense.Total)
            {
                ModelState.AddModelError("Payer", "The  total amount for this expense and the total amount by payers' distribution differ.");
                return null;
            }
            #endregion
            #endregion

            var result = _expenses.Add(entity);
            if (result != null)
                UpdateBalances(entity.Items.SelectMany(x => x.Payers), entity.Payee, entity.Total);

            return result;
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
    }
}
