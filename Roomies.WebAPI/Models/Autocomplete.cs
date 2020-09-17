using System;
using System.Collections.Generic;

namespace Roomies.WebAPI.Models
{
    public class Autocomplete : Entity, IEquatable<Autocomplete>
    {
        public string Text { get; set; }
        public AutocompleteType Type { get; set; }

        public override bool Equals(object obj) => Equals(obj as Autocomplete);

        public bool Equals(Autocomplete other)
            =>  other != null &&
                Id == other.Id &&
                Text == other.Text &&
                Type == other.Type;

        public override int GetHashCode() => HashCode.Combine(Id, Text, Type);

        public static bool operator ==(Autocomplete left, Autocomplete right)
            => EqualityComparer<Autocomplete>.Default.Equals(left, right);

        public static bool operator !=(Autocomplete left, Autocomplete right)
            => !(left == right);
    }

    public enum AutocompleteType
    {
        BusinessName, ItemName
    }
}
