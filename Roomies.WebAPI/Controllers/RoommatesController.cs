using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Roomies.WebAPI.Extensions;
using Roomies.WebAPI.Models;
using Roomies.WebAPI.Repositories.Interfaces;
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
                Expenses = expenses,
                YourTotal = expenses.TotalForPayer(roommate.Id)
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
            {
                result.Expense = simple;
                result.YourTotal = simple.Payers.SingleOrDefault(x => x.Id == roommate.Id).Amount;
            }
            else if (expense is DetailedExpense detailed)
            {
                detailed.Items = detailed.Items.Where(x => x.Payers.Any(x => x.Id == roommate.Id));
                result.Expense = detailed.Items.Any() ? detailed : null;
                result.YourTotal = detailed.Items.Sum(i => i.Payers.SingleOrDefault(x => x.Id == roommate.Id).Amount);
            }
            
            if (result.Expense != null)
                return Ok(result);

            return NotFound();
        }

        // GET: api/roommates/{roommateId}/payments
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpGet("{id}/Payments")]
        public ActionResult<IEnumerable<Payment>> GetPayments(string id)
        {
            var roommate = _roommates.Get(id);
            if (roommate == null)
                return NotFound();

            var result = _payments.Get(roommate);
            if (result != null)
                return Ok(result);

            return NotFound();
        }

    }
}
