using System.Collections.Generic;
using Roomies.App.Models;

namespace Roomies.App.Persistence.Interfaces
{
    public interface IAutocompleteRepository
    {
        void Index(IEnumerable<Autocomplete> autocomplete);
        IEnumerable<string> Search(string text, AutocompleteType? type);
    }
}
