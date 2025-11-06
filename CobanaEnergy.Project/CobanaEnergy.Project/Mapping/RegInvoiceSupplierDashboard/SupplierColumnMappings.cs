using System.Linq;

namespace CobanaEnergy.Project.Common
{
    public static class SupplierColumnMappings
    {
        // Unified column mappings - all possible column headers for each DTO property
        private static readonly string[] _businessNameColumns = new[]
        {
            "customername", "customer_name",
            "businessname", "business_name",
            "companyname", "company_name",
            "billingaddress2", "billing_address2"
        };

        private static readonly string[] _mpxnColumns = new[]
        {
            "mpxn",
            "mpan",
            "mprn",
            "mpan/mprn",
            "mpanmprn"
        };

        private static readonly string[] _inputDateColumns = new[]
        {
            "dateofsale", "date_of_sale",
            "agreementreceiveddate", "agreement_received_date",
            "contractwondate", "contract_won_date",
            "dateofsalerenewal", "date_of_sale_renewal"
        };

        private static readonly string[] _supplierNameColumns = new[]
        {
            "suppliername", "supplier_name"
        };

        private static readonly string[] _postCodeColumns = new[]
        {
            "postcode", "post_code"
        };

        public static string NormalizeHeader(string header)
        {
            if (string.IsNullOrWhiteSpace(header))
                return string.Empty;

            return header
                .Replace("\u00A0", " ")
                .Replace("\u200B", "")
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("-", "")
                .Trim()
                .ToLowerInvariant();
        }

        public static bool IsBusinessNameColumn(string header)
        {
            var normalized = NormalizeHeader(header);
            return _businessNameColumns.Any(col => NormalizeHeader(col) == normalized);
        }

        public static bool IsMpxnColumn(string header)
        {
            var normalized = NormalizeHeader(header);
            return _mpxnColumns.Any(col => NormalizeHeader(col) == normalized);
        }

        public static bool IsInputDateColumn(string header)
        {
            var normalized = NormalizeHeader(header);
            return _inputDateColumns.Any(col => NormalizeHeader(col) == normalized);
        }

        public static bool IsSupplierNameColumn(string header)
        {
            var normalized = NormalizeHeader(header);
            return _supplierNameColumns.Any(col => NormalizeHeader(col) == normalized);
        }

        public static bool IsPostCodeColumn(string header)
        {
            var normalized = NormalizeHeader(header);
            return _postCodeColumns.Any(col => NormalizeHeader(col) == normalized);
        }

        // Helper method to get all recognized columns for debugging
        public static string[] GetAllBusinessNameColumns() => _businessNameColumns;
        public static string[] GetAllMpxnColumns() => _mpxnColumns;
        public static string[] GetAllInputDateColumns() => _inputDateColumns;
        public static string[] GetAllSupplierNameColumns() => _supplierNameColumns;
        public static string[] GetAllPostCodeColumns() => _postCodeColumns;
    }
}
