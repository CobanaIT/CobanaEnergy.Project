using System;
using System.Globalization;

namespace CobanaEnergy.BackgroundServices.Helpers
{
    /// <summary>
    /// General-purpose helper class with reusable static methods
    /// Provides utility functions for common operations across the application
    /// </summary>
    public static class Helper
    {
        /// <summary>
        /// Safely parses a date string using multiple common formats
        /// Tries exact parsing first, then falls back to general parsing
        /// </summary>
        /// <param name="dateString">The date string to parse</param>
        /// <param name="result">The parsed DateTime if successful</param>
        /// <returns>True if parsing was successful, false otherwise</returns>
        public static bool TryParseStartDate(string dateString, out DateTime result)
        {
            // Try multiple common date formats
            string[] formats = {
                "yyyy-MM-dd",
                "dd/MM/yyyy",
                "MM/dd/yyyy",
                "dd-MM-yyyy",
                "MM-dd-yyyy",
                "yyyy/MM/dd"
            };

            // Try exact parsing first with specific formats
            if (DateTime.TryParseExact(
                dateString,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out result))
            {
                return true;
            }

            // Fall back to general parsing
            return DateTime.TryParse(dateString, out result);
        }
    }
}

