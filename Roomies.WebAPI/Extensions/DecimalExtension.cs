using System;
namespace Roomies.WebAPI.Extensions
{
    internal static class DecimalExtension
    {
        internal static decimal Rounded(this decimal value, int places)
            => decimal.Round(value, places, MidpointRounding.ToPositiveInfinity);
    }
}
