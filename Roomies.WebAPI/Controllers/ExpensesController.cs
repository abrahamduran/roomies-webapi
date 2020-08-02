using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Roomies.WebAPI.Extensions;
using Roomies.WebAPI.Models;
using Roomies.WebAPI.Repositories.Interfaces;
using Roomies.WebAPI.Requests;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Roomies.WebAPI.Controllers
{
    [Route("api/[controller]")]
    public class ExpensesController : Controller
    {
        private readonly IExpensesRepository _expenses;
        private readonly IRoommatesRepository _roommates;

        public ExpensesController(IExpensesRepository expenses, IRoommatesRepository roommates)
        {
            _expenses = expenses;
            _roommates = roommates;
        }

        // GET: api/expenses
        [HttpGet]
        public IEnumerable<Expense> Get()
        {
            return _expenses.GetExpenses();
        }

        // POST api/expenses
        [HttpPost]
        public ActionResult<Expense> Post([FromBody] RegisterExpense expense)
        {
            if (ModelState.IsValid)
            {
                var entity = (Expense)expense;

                #region Validate payee
                var roommate = _roommates.GetById(expense.PayeeId);
                if (roommate == null)
                {
                    ModelState.AddModelError("PayeeId", "The specified PayeeId is not valid or does not represent a registered Roommate.");
                    return BadRequest(ModelState);
                }
                entity.Payee = new Payee { Id = roommate.Id, Name = roommate.Name };
                #endregion

                #region Validate payers
                var payers = _roommates.GetByIds(expense.Payers.Select(x => x.Id));
                if (payers.Count() != expense.Payers.Count())
                {
                    ModelState.AddModelError("Payer", "At least one Payer is invalid or does not represent a registered Roommate.");
                    return BadRequest(ModelState);
                }
                var sum = expense.Payers.Sum(x => x.Multiplier * (double)x.Amount);
                if (sum > 0)
                {
                    ModelState.AddModelError("Payer", "Amount and Multiplier cannot be filled at the same time. Please, select only one.");
                    return BadRequest(ModelState);
                }
                entity.Payers = expense.Payers.Select(x => new Payer
                {
                    Id = x.Id,
                    Amount = expense.Distribution.GetAmount(expense, x),
                    Name = payers.Single(p => p.Id == x.Id).Name
                });
                var total = entity.Payers.Sum(x => x.Amount);
                if (total != expense.Total)
                {
                    ModelState.AddModelError("Payer", "The Amount and the Total Distribution Amount differ.");
                    return BadRequest(ModelState);
                }
                #endregion

                #region Update roommates' balances
                UpdateBalances(entity.Payers, entity.Payee, entity.Total);
                #endregion

                // validate items/amount

                var result = _expenses.Add(entity);
                if (result != null)
                    return Created("api/expenses", result);
            }

            return BadRequest(ModelState);
        }

        private void UpdateBalances(IEnumerable<Payer> payers, Payee payee, decimal total)
        {
            (bool isIncluded, decimal amount) payeeInfo = (false, 0);

            foreach (var payer in payers)
            {
                payeeInfo.isIncluded = payer.Id == payee.Id;
                if (payeeInfo.isIncluded)
                {
                    payeeInfo.amount = payer.Amount;
                    continue;
                }

                _roommates.UpdateBalance(payer.Id, payer.Amount);
            }

            if (payeeInfo.isIncluded)
                _roommates.UpdateBalance(payee.Id, -total + payeeInfo.amount);
            else
                _roommates.UpdateBalance(payee.Id, -total);
        }
    }
}
