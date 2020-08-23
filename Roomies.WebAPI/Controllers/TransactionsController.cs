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
    public class TransactionsController : Controller
    {
        private readonly ITransactionsRepository _repository;

        public TransactionsController(ITransactionsRepository repository)
        {
            _repository = repository;
        }

        // GET: api/transactions
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<Transaction>> Get() => Ok(_repository.Get());
    }
}
