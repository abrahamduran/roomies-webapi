using System;
using System.Collections.Generic;
using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Roomies.App.Models;
using Roomies.App.Persistence.Interfaces;
using Roomies.WebAPI.Extensions;
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
        {
            if (string.IsNullOrEmpty(text))
                return BadRequest("Text cannot be empty or null. Please provide a value");

            return Ok(_autocomplete.Search(text, GetFieldType(field)));
        }

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

        internal AutocompleteType? GetFieldType(AutocompletableField field)
        {
            switch (field)
            {
                case AutocompletableField.BusinessName:
                    return AutocompleteType.BusinessName;
                case AutocompletableField.ItemName:
                    return AutocompleteType.ItemName;
                case AutocompletableField.All:
                    return null;
            }
            throw new NotImplementedException($"AutocompletableField case {field} was not properly handled in GetFieldType.");
        }

        internal AutocompleteType GetFieldType(IndexAutocompletableField field)
        {
            switch (field)
            {
                case IndexAutocompletableField.BusinessName:
                    return AutocompleteType.BusinessName;
                case IndexAutocompletableField.ItemName:
                    return AutocompleteType.ItemName;
            }
            throw new NotImplementedException($"AutocompletableField case {field} was not properly handled in GetFieldType.");
        }
    }
}
