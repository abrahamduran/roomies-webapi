using System.Collections.Generic;
using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Roomies.App.Models;
using Roomies.App.Persistence.Interfaces;

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
