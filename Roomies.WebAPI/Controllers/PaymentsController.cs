using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Roomies.App.Models;
using Roomies.App.Persistence.Interfaces;
using Roomies.App.UseCases;
using Roomies.App.UseCases.RegisterPayment;
using Roomies.WebAPI.Extensions;
using Roomies.WebAPI.Requests;
using Roomies.WebAPI.Responses;

namespace Roomies.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ApiConventionType(typeof(DefaultApiConventions))]
    public class PaymentsController : Controller
    {
        private const decimal MAX_OFFSET_VALUE = 0.1M;

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
        public ActionResult<IEnumerable<PaymentResult>> Get() => Ok(_payments.Get().Select(toResponse).ToList());

        // GET api/payments/5
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<PaymentResult> Get(string id)
        {
            var result = _payments.Get(id);
            if (result != null) return Ok(toResponse(result, true));

            return NotFound();
        }

        // POST api/payments
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(Dictionary<string, string[]>), StatusCodes.Status400BadRequest)]
        public ActionResult<RegisterPaymentResponse> Post([FromServices] RegisterPaymentHandler handler, [FromBody] RegisterPaymentRequest payment)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = handler.Execute(payment);
                return CreatedAtAction(nameof(Post), new { id = result.Id }, result);
            }
            catch (UseCaseException ex)
            {
                return BadRequest(ex.ToModelState(ModelState));
            }
        }

        //// PUT api/payments/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody] string value)
        //{
        //}

        // DELETE api/payments/5
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult Delete(string id)
        {
            var payment = _payments.Get(id);
            if (payment == null) return NotFound();

            _expenses.UnsetPayment(payment.Id, payment.Expenses);
            _roommates.UpdateBalance(payment.By.Id, payment.Total);
            _roommates.UpdateBalance(payment.To.Id, -payment.Total);

            return NoContent();
        }

        private PaymentResult toResponse(Payment payment) => toResponse(payment, false);
        private PaymentResult toResponse(Payment payment, bool includesExpenses)
            => PaymentResult.ForPayment(payment, includesExpenses);
    }
}
