using System.Collections.Generic;
using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Roomies.WebAPI.Models;
using Roomies.WebAPI.Repositories.Interfaces;
using Roomies.WebAPI.Requests;

namespace Roomies.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ApiConventionType(typeof(DefaultApiConventions))]
    public class RoommatesController : Controller
    {
        private readonly IRoommatesRepository _repository;

        public RoommatesController(IRoommatesRepository repository)
        {
            _repository = repository;
        }

        // GET api/roommates
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<Roommate>> Get() => Ok(_repository.Get());

        // GET api/roommates/{id}
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<Roommate> Get(string id)
        {
            var result = _repository.GetById(id);
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
                var result = _repository.Add(roommate);
                if (result != null)
                    return CreatedAtAction(nameof(Post), new { id = result.Id }, result);
            }

            return BadRequest(ModelState);
        }

        /// Handle the update of a roomie, reference in transactions should be updated as well
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
    }
}
