using System;
using Roomies.WebAPI.Models;
using Roomies.WebAPI.Requests;

namespace Roomies.WebAPI.Extensions
{
    internal static class AutocompletableFieldExtension
    {
        internal static AutocompleteType? GetFieldType(this AutocompletableField field)
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

        internal static AutocompleteType GetFieldType(this IndexAutocompletableField field)
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