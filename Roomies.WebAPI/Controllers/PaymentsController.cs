using System.Collections.Generic;
using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Roomies.WebAPI.Models;
using Roomies.WebAPI.Repositories.Interfaces;

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

        public PaymentsController(IPaymentsRepository payments)
        {
            _payments = payments;
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
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<Payment> Post([FromBody] Payment payment)
        {
            if (ModelState.IsValid)
            {
                var result = _payments.Add(payment);
                if (result != null)
                    return CreatedAtAction(nameof(Post), new { id = result.Id }, result);
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
