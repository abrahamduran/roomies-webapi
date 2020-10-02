using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
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
    public class PaymentsController : Controller
    {
        private readonly IPaymentsRepository _payments;
        private readonly IExpensesRepository _expenses;
        private readonly IRoommatesRepository _roommates;

        public PaymentsController(IPaymentsRepository payments, IExpensesRepository expenses, IRoommatesRepository roommates)
        {
            _payments = payments;
            _expenses = expenses;
            _roommates = roommates;
        }

        // GET: api/payments
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<Payment>> Get() => Ok(_payments.Get());

        // GET api/payments/5
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<Payment> Get(string id)
        {
            var result = _payments.Get(id);
            if (result != null) return Ok(result);

            return NotFound();
        }

        // POST api/payments
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(Dictionary<string, string[]>), StatusCodes.Status400BadRequest)]
        public ActionResult<Payment> Post([FromBody] RegisterPayment payment)
        {
            if (ModelState.IsValid)
            {
                var roommates = _roommates.Get(new[] { payment.PaidTo, payment.PaidBy }).ToDictionary(x => x.Id);
                if (!roommates.ContainsKey(payment.PaidBy))
                    ModelState.AddModelError("PaidBy", "The PaidBy field is invalid. Please review it.");
                if (!roommates.ContainsKey(payment.PaidTo))
                    ModelState.AddModelError("PaidTo", "The PaidBy field is invalid. Please review it.");

                var expenses = _expenses.Get(payment.ExpenseIds);
                if (expenses.Count() != payment.ExpenseIds.Count())
                {
                    ModelState.AddModelError("ExpenseIds", "One or more expenses are invalid. Please review them before submission.");
                    return BadRequest(ModelState);
                }

                if (!expenses.ContainsPayer(payment.PaidBy))
                {
                    ModelState.AddModelError("PaidBy", "The selected payer is invalid for the selected expenses.");
                    ModelState.AddModelError("ExpenseIds", "At least one expense does not contains the selected payer.");
                }
                if (!expenses.ContainsPayee(payment.PaidTo))
                {
                    ModelState.AddModelError("PaidTo", "The selected payee is invalid for the selected expenses.");
                    ModelState.AddModelError("ExpenseIds", "At least one expense does not contains the selected payee.");
                    return BadRequest(ModelState);
                }

                var totalExpense = expenses.TotalForPayer(payment.PaidBy);
                if (totalExpense != payment.Amount)
                {
                    ModelState.AddModelError("Amount", "The amount introduced does not match with the total amount for the selected expenses.");
                    ModelState.AddModelError("Amount", "As of now, partial payments are yet to be supported.");
                    return BadRequest(ModelState);
                }

                var entity = new Payment
                {
                    By = new Payee { Id = payment.PaidBy, Name = roommates[payment.PaidBy].Name },
                    To = new Payee { Id = payment.PaidTo, Name = roommates[payment.PaidTo].Name },
                    Expenses = expenses.Select(x => (Expense.Summary)x).ToList(),
                    Description = payment.Description,
                    Total = payment.Amount,
                    Date = DateTime.Now
                };

                var result = _payments.Add(entity);
                if (result != null)
                {
                    _roommates.UpdateBalance(payment.PaidBy, -payment.Amount);
                    _roommates.UpdateBalance(payment.PaidTo, payment.Amount);
                    _expenses.SetStatus(expenses.Select(x => x.Id).ToList(), ExpenseStatus.Paid);
                    return CreatedAtAction(nameof(Post), new { id = result.Id }, result);
                }
            }

            return BadRequest(ModelState);
        }

        //// PUT api/payments/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody] string value)
        //{
        //}

        //// DELETE api/payments/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}
    }
}
