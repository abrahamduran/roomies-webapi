using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Roomies.App.UseCases;

namespace Roomies.WebAPI.Extensions
{
    internal static class UseCaseExceptionExtension
    {
        internal static ModelStateDictionary ToModelState(this UseCaseException exception, ModelStateDictionary modelState)
        {
            foreach (var error in exception.Errors)
                foreach (var value in error.Value)
                    modelState.AddModelError(error.Key, value);

            return modelState;
        }
    }
}
