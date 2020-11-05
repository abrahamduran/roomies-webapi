using System;
namespace Roomies.WebAPI.Extensions
{
    public static class DecimalExtension
    {
        internal static decimal Rounded(this decimal value, int places)
            => decimal.Round(value, places, MidpointRounding.ToPositiveInfinity);
    }
}
