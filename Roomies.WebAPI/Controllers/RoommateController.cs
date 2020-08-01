using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Roomies.WebAPI.Models;
using Roomies.WebAPI.Repositories;
using Roomies.WebAPI.Requests;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Roomies.WebAPI.Controllers
{
    [Route("api/[controller]")]
    public class RoomiesController : Controller
    {
        private readonly RoommatesService _service;

        public RoomiesController(RoommatesService service)
        {
            _service = service;
        }

        [HttpGet]
        public IEnumerable<Roommate> Get() => _service.Get();

        //[HttpGet("{id}")]
        //public Roomie Get(int id)
        //{
        //    return "value";
        //}

        [HttpPost]
        public ActionResult<Roommate> Post([FromBody] CreateRoomie roomie)
        {
            if (ModelState.IsValid)
            {
                var result = _service.Add(roomie);
                if (result != null)
                    return Created("/roomies", result);
            }

            return BadRequest(ModelState);
        }

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
