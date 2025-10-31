using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Service.ExtensionService
{
    public static class StringExtensions
    {
        public static string ToTwoDecimal(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty; // or return value; if you want to keep original null/empty

            if (decimal.TryParse(value, out var dec))
                return dec.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

            return value; // return original if not a valid number
        }   
    }
}