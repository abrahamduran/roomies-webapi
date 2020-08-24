using System.Collections.Generic;
using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Roomies.WebAPI.Extensions;
using Roomies.WebAPI.Models;
using Roomies.WebAPI.Repositories.Interfaces;
using Roomies.WebAPI.Requests;

namespace Roomies.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ApiConventionType(typeof(DefaultApiConventions))]
    public class AutocompleteController : Controller
    {
        private readonly IAutocompleteRepository _autocomplete;

        public AutocompleteController(IAutocompleteRepository autocomplete)
        {
            _autocomplete = autocomplete;
        }

        // GET: api/values
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<string>> Get(string text, AutocompletableField field = AutocompletableField.All)
            => Ok(_autocomplete.Search(text, field.GetFieldType()));

        // POST api/values
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(Dictionary<string, string[]>), StatusCodes.Status400BadRequest)]
        public ActionResult<Autocomplete> Post([FromBody] IndexAutocompletableText autocomplete)
        {
            if (ModelState.IsValid)
            {
                var type = autocomplete.Field.GetFieldType();
                _autocomplete.Index(new[] { new Autocomplete { Text = autocomplete.Text, Type = type } });
                return Created(nameof(Post), autocomplete);
            }

            return BadRequest(ModelState);
        }
    }
}
