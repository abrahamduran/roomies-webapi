using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Roomies.WebAPI.Extensions;
using Roomies.WebAPI.Models;
using Roomies.WebAPI.Repositories.Interfaces;
using Roomies.WebAPI.Requests;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

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
            var result = _payments.GetById(id);
            if (result != null) return Ok(result);

            return NotFound(id);
        }

        // POST api/payments
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(Dictionary<string, string[]>), StatusCodes.Status400BadRequest)]
        public ActionResult<Payment> Post([FromBody] RegisterPayment payment)
        {
            if (ModelState.IsValid)
            {
                var roommates = _roommates.GetByIds(new[] { payment.PaidTo, payment.PaidBy }).ToDictionary(x => x.Id);
                if (!roommates.ContainsKey(payment.PaidBy))
                    ModelState.AddModelError("PaidBy", "The PaidBy field is invalid. Please review it.");
                if (!roommates.ContainsKey(payment.PaidTo))
                    ModelState.AddModelError("PaidTo", "The PaidBy field is invalid. Please review it.");

                var expenses = _expenses.GetByIds(payment.ExpenseIds);
                if (expenses.Count() != payment.ExpenseIds.Count())
                    ModelState.AddModelError("ExpenseIds", "One or more expenses are invalid. Please review them before submission.");

                expenses.ContainsPayer(payment.PaidBy);
                var totalExpense = expenses.Sum(x => x.Total);
                if (totalExpense != payment.Amount)
                    ModelState.AddModelError("Total", "The total amount introduced does not match with the total amount for the expenses selected.");
                    ModelState.AddModelError("Total", "As of now, partial payments are yet to be supported.");

            }

            if (ModelState.IsValid)
            {
                //if (result != null)
                    //return CreatedAtAction(nameof(Post), new { id = result.Id }, result);
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
