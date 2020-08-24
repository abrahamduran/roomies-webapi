using System.Collections.Generic;
using Roomies.WebAPI.Models;

namespace Roomies.WebAPI.Repositories.Interfaces
{
    public interface IAutocompleteRepository
    {
        void Index(IEnumerable<Autocomplete> autocomplete);
        IEnumerable<string> Search(string text, AutocompleteType? type);
    }
}
