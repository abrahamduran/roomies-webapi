using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Roomies.App.Models;
using Roomies.App.Persistence.Interfaces;
using Roomies.WebAPI.Requests;
using Roomies.WebAPI.Responses;

namespace Roomies.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ApiConventionType(typeof(DefaultApiConventions))]
    public class RoommatesController : Controller
    {
        private readonly IRoommatesRepository _roommates;
        private readonly IExpensesRepository _expenses;
        private readonly IPaymentsRepository _payments;

        public RoommatesController(IRoommatesRepository roommates, IExpensesRepository expenses, IPaymentsRepository payments)
        {
            _roommates = roommates;
            _expenses = expenses;
            _payments = payments;
        }

        // GET api/roommates
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<Roommate>> Get() => Ok(_roommates.Get());

        // GET api/roommates/{id}
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<Roommate> Get(string id)
        {
            var result = _roommates.Get(id);
            if (result != null) return Ok(result);

            return NotFound();
        }

        // POST api/roommates
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(Dictionary<string, string[]>), StatusCodes.Status400BadRequest)]
        public ActionResult<Roommate> Post([FromBody] CreateRoommate roommate)
        {
            // TODO: allow CreateRoommate to specify the username
            // TODO: manage this exception returning a bad request with the appropiate error message
            // produces an error when an email is duplicated, something similar might happen with the username
            // MongoDB.Driver.MongoWriteException: A write operation resulted in an error.
            // E11000 duplicate key error collection: roomies.roommates index: email_1 dup key: { email: "user@example.com" }
            if (ModelState.IsValid)
            {
                var result = _roommates.Add(roommate);
                if (result != null)
                    return CreatedAtAction(nameof(Post), new { id = result.Id }, result);
            }

            return BadRequest(ModelState);
        }

        /// Handle the update of a roomie, references in transactions should be updated as well
        /// https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-2.1&tabs=visual-studio#queued-background-tasks-1
        // PUT api/roommates
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody] UpdateRoommate roommate)
        //{
        //}

        // DELETE api/roommates
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}

        // GET: api/roommates/{roommateId}/expenses
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpGet("{id}/Expenses")]
        public ActionResult<RoommateExpenses> GetExpenses(string id)
        {
            var roommate = _roommates.Get(id);
            if (roommate == null)
                return NotFound();

            var expenses = _expenses.Get(roommate);
            if (expenses == null)
                return NotFound();

            var result = new RoommateExpenses
            {
                Expenses = expenses.Select(x => ExpenseResult.ForPayer(x, roommate.Id)).ToList(),
            };
            return Ok(result);
        }

        // GET api/roommates/{roommateId}/expenses/{id}
        [HttpGet("{roommateId}/Expenses/{expenseId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<RoommateExpense> GetExpense(string roommateId, string expenseId)
        {
            var expense = _expenses.Get(expenseId);
            var roommate = _roommates.Get(roommateId);
            if (roommate == null || expense == null)
                return NotFound();

            var result = new RoommateExpense();
            if (expense is SimpleExpense simple && simple.Payers.Any(x => x.Id == roommate.Id))
                result.Expense = ExpenseResult.ForPayer(simple, roommate.Id);
            else if (expense is DetailedExpense detailed)
                result.Expense = ExpenseResult.ForPayer(detailed, roommate.Id);
            
            if (result.Expense != null) return Ok(result);

            return NotFound();
        }

        // GET: api/roommates/{roommateId}/payments
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpGet("{id}/Payments")]
        public ActionResult<IEnumerable<RoommatePayment>> GetPayments(string id)
        {
            var roommate = _roommates.Get(id);
            if (roommate == null)
                return NotFound();

            var result = _payments.Get(roommate);
            if (result != null)
                return Ok(new RoommatePayments
                {
                    Payments = result.Select(x => PaymentResult.ForRoommate(x)).ToList(),
                    YourTotal = result.Sum(x => x.Total)
                });

            return NotFound();
        }

        // GET: api/roommates/{roommateId}/payments/{paymentId}
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpGet("{roommateId}/Payments/{paymentId}")]
        public ActionResult<IEnumerable<RoommatePayment>> GetPayment(string roommateId, string paymentId)
        {
            var roommate = _roommates.Get(roommateId);
            var payment = _payments.Get(paymentId);
            
            if (roommate == null || payment == null)
                return NotFound();

            if (payment.By != roommate)
                return NotFound();

            return Ok(new RoommatePayment
            {
                Payment = PaymentResult.ForRoommate(payment),
                YourTotal = payment.Total
            });
        }
    }
}
