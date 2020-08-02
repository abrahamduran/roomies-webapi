using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Roomies.WebAPI.Models;
using Roomies.WebAPI.Repositories.Implementations;
using Roomies.WebAPI.Repositories.Interfaces;
using Roomies.WebAPI.Requests;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Roomies.WebAPI.Controllers
{
    [Route("api/[controller]")]
    public class RoomiesController : Controller
    {
        private readonly IRoommatesRepository _repository;

        public RoomiesController(IRoommatesRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public IEnumerable<Roommate> Get() => _repository.Get();

        [HttpGet("{id}")]
        public ActionResult<Roommate> Get(string id)
        {
            var result = _repository.GetById(id);
            if (result != null) return result;

            return NotFound(id);
        }

        [HttpPost]
        public ActionResult<Roommate> Post([FromBody] CreateRoomie roomie)
        {
            if (ModelState.IsValid)
            {
                var result = _repository.Add(roomie);
                if (result != null)
                    return Created("api/roomies", result);
            }

            return BadRequest(ModelState);
        }

        /// Handle the update of a roomie, reference in transactions should be updated as well
        /// https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-2.1&tabs=visual-studio#queued-background-tasks-1
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody] Roomie roomie)
        //{
        //}

        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}
    }
}
