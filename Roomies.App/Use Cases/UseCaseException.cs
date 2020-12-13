using System;
using System.Collections.Generic;

namespace Roomies.App.UseCases
{
    public class UseCaseException: ApplicationException
    {
        public Dictionary<string, IList<string>> Errors { get; set; }

        public UseCaseException() : base("An Use Case Exception has ocurred.")
        {
            Errors = new Dictionary<string, IList<string>>();
        }

        public UseCaseException(string field, string message) : base("An Use Case Exception has ocurred.")
        {
            Errors = new Dictionary<string, IList<string>> { { field, new[] { message } } };
        }

        public void AddError(string field, string message)
        {
            if (Errors.ContainsKey(field))
                Errors[field].Add(message);
            else
                Errors[field] = new List<string> { message };
        }
    }
}
