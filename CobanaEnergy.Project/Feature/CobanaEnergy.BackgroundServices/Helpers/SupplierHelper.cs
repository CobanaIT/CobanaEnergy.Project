using System.Collections.Generic;

namespace CobanaEnergy.BackgroundServices.Helpers
{
    /// <summary>
    /// Helper class for supplier-related operations
    /// Contains hardcoded supplier dictionary and maximum objection counts
    /// </summary>
    public static class SupplierHelper
    {
        /// <summary>
        /// Hardcoded dictionary mapping SupplierID to Supplier Name
        /// Supplier IDs will never change
        /// </summary>
        private static readonly Dictionary<long, string> SupplierDictionary = new Dictionary<long, string>
        {
            { 27, "BG Lite" },
            { 29, "British Gas Business" },
            { 31, "EDF I&C" },
            { 43, "EDF SME" },
            { 44, "Corona" },
            { 10061, "Crown Gas and Power" },
            { 10062, "Scottish Power" },
            { 10063, "Smartest Energy" },
            { 10064, "SSE" },
            { 10065, "Total Gas & Power" },
            { 10066, "SEFE" }
        };

        /// <summary>
        /// Hardcoded dictionary mapping Supplier Name to Maximum Objection Count
        /// </summary>
        private static readonly Dictionary<string, int> MaxObjectionCounts = new Dictionary<string, int>
        {
            { "BG Lite", 4 },
            { "EDF I&C", 4 },
            { "EDF SME", 4 },
            { "Smartest Energy", 6 },
            { "Scottish Power", 3 },
            { "SSE", 2 }
        };

        /// <summary>
        /// Default maximum objection count for suppliers not in the dictionary
        /// </summary>
        private const int DefaultMaxObjectionCount = 4;

        /// <summary>
        /// Gets the supplier name by SupplierID
        /// </summary>
        /// <param name="supplierId">The supplier ID</param>
        /// <returns>Supplier name if found, null otherwise</returns>
        public static string GetSupplierName(long supplierId)
        {
            return SupplierDictionary.TryGetValue(supplierId, out string name) ? name : null;
        }

        /// <summary>
        /// Gets the maximum objection count for a supplier by SupplierID
        /// Returns the supplier-specific max count or default (4) if not found
        /// </summary>
        /// <param name="supplierId">The supplier ID</param>
        /// <returns>Maximum objection count for the supplier</returns>
        public static int GetMaxObjectionCount(long supplierId)
        {
            var supplierName = GetSupplierName(supplierId);
            if (supplierName == null)
            {
                return DefaultMaxObjectionCount;
            }

            return MaxObjectionCounts.TryGetValue(supplierName, out int maxCount) 
                ? maxCount 
                : DefaultMaxObjectionCount;
        }

        /// <summary>
        /// Gets the maximum objection count for a supplier by Supplier Name
        /// </summary>
        /// <param name="supplierName">The supplier name</param>
        /// <returns>Maximum objection count for the supplier</returns>
        public static int GetMaxObjectionCountByName(string supplierName)
        {
            if (string.IsNullOrEmpty(supplierName))
            {
                return DefaultMaxObjectionCount;
            }

            return MaxObjectionCounts.TryGetValue(supplierName, out int maxCount) 
                ? maxCount 
                : DefaultMaxObjectionCount;
        }
    }
}



