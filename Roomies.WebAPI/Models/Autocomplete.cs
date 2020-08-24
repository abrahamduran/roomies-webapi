using System;
namespace Roomies.WebAPI.Models
{
    public class Autocomplete : Entity
    {
        public string Text { get; set; }
        public AutocompleteType Type { get; set; }
    }

    public enum AutocompleteType
    {
        BusinessName, ItemName
    }
}
