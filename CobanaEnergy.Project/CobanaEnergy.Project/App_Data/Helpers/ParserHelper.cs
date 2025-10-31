using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Data.Entity;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.Sector.SectorDBModels;
using CobanaEnergy.Project.Models.Electric.ElectricDBModels;
using CobanaEnergy.Project.Models.Gas.GasDBModels;

namespace CobanaEnergy.Project.Helpers
{
    /// <summary>
    /// Helper class for parsing and formatting data
    /// </summary>
    public static class ParserHelper
    {
        /// <summary>
        /// Format date string from dd/MM/yyyy or yyyy-MM-dd to dd-MM-yy for display
        /// </summary>
        /// <param name="dateString">The date string to format</param>
        /// <returns>Formatted date string in dd-MM-yy format or "N/A" if invalid</returns>
        public static string FormatDateForDisplay(string dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString) || dateString == "N/A" || dateString == "-")
                return "N/A";

            // Try dd/MM/yyyy format first (primary format)
            if (DateTime.TryParseExact(dateString, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
                return result.ToString("dd-MM-yy");

            // Try yyyy-MM-dd format (ISO format)
            if (DateTime.TryParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                return result.ToString("dd-MM-yy");

            // Try dd-MM-yyyy format (already formatted)
            if (DateTime.TryParseExact(dateString, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                return result.ToString("dd-MM-yy");

            return dateString; // Return original if parsing fails
        }

        /// <summary>
        /// Parse date string in various formats to DateTime for sorting
        /// </summary>
        /// <param name="dateString">The date string to parse</param>
        /// <returns>Parsed DateTime or DateTime.MinValue if invalid</returns>
        public static DateTime ParseDateForSorting(string dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString) || dateString == "N/A" || dateString == "-")
                return DateTime.MinValue;

            // Define all possible date formats
            string[] formats = {
                "dd/MM/yyyy",
                "yyyy-MM-dd",
                "dd-MM-yyyy",
                "dd-MM-yy",
                "dd/MM/yy",
                "yyyy/MM/dd",
                "MM/dd/yyyy",
                "MM-dd-yyyy"
            };

            // Try parsing with each format
            foreach (string format in formats)
            {
                if (DateTime.TryParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
                    return result;
            }

            // Try general parsing as fallback
            if (DateTime.TryParse(dateString, out DateTime generalResult))
                return generalResult;

            return DateTime.MinValue;
        }

        /// <summary>
        /// Format date string to dd-MM-yy HH:mm:ss format for display
        /// </summary>
        /// <param name="dateString">The date string to format</param>
        /// <returns>Formatted date and time string in dd-MM-yy HH:mm:ss format or "N/A" if invalid</returns>
        public static string FormatDateTimeForDisplay(string dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString) || dateString == "N/A" || dateString == "-")
                return "N/A";

            // Try various date formats and parse to DateTime
            string[] formats = {
                "dd/MM/yyyy HH:mm:ss",
                "dd/MM/yyyy",
                "yyyy-MM-dd HH:mm:ss",
                "yyyy-MM-dd",
                "dd-MM-yyyy HH:mm:ss",
                "dd-MM-yyyy",
                "MM/dd/yyyy HH:mm:ss",
                "MM/dd/yyyy"
            };

            foreach (string format in formats)
            {
                if (DateTime.TryParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
                    return result.ToString("dd-MM-yy HH:mm:ss");
            }

            // Try general parsing as fallback
            if (DateTime.TryParse(dateString, out DateTime generalResult))
                return generalResult.ToString("dd-MM-yy HH:mm:ss");

            return "N/A";
        }

        /// <summary>
        /// Populate department-based fields for Electric contracts
        /// </summary>
        /// <param name="contract">The electric contract</param>
        /// <param name="departmentFields">Dictionary to populate with field values</param>
        /// <param name="db">Database context</param>
        /// <returns>Task representing the async operation</returns>
        public static async Task PopulateDepartmentFieldsForElectric(CE_ElectricContracts contract, System.Collections.Generic.Dictionary<string, string> departmentFields, ApplicationDBContext db)
        {
            try
            {
                if (contract == null) return;

                string department = contract.Department;
                if (string.IsNullOrWhiteSpace(department)) return;

                switch (department.ToUpper())
                {
                    case "BROKERS":
                        // Populate Brokerage Staff
                        if (contract.BrokerageStaffId.HasValue && contract.BrokerageStaffId.Value > 0)
                        {
                            var brokerageStaff = await db.CE_BrokerageStaff
                                .FirstOrDefaultAsync(bs => bs.BrokerageStaffID == contract.BrokerageStaffId.Value);
                            departmentFields["BrokerageStaffName"] = brokerageStaff?.BrokerageStaffName ?? "N/A";
                        }
                        else
                        {
                            departmentFields["BrokerageStaffName"] = "N/A";
                        }

                        // Populate Sub Brokerage
                        if (contract.SubBrokerageId.HasValue && contract.SubBrokerageId.Value > 0)
                        {
                            var subBrokerage = await db.CE_SubBrokerage
                                .FirstOrDefaultAsync(sb => sb.SubBrokerageID == contract.SubBrokerageId.Value);
                            departmentFields["SubBrokerageName"] = subBrokerage?.SubBrokerageName ?? "N/A";
                        }
                        else
                        {
                            departmentFields["SubBrokerageName"] = "N/A";
                        }
                        break;

                    case "IN HOUSE":
                        // Populate Closer
                        if (contract.CloserId.HasValue && contract.CloserId.Value > 0)
                        {
                            var closer = await db.CE_Sector
                                .FirstOrDefaultAsync(s => s.SectorID == contract.CloserId.Value);
                            departmentFields["CloserName"] = closer?.Name ?? "N/A";
                        }
                        else
                        {
                            departmentFields["CloserName"] = "N/A";
                        }

                        // Populate Lead Generator
                        if (contract.LeadGeneratorId.HasValue && contract.LeadGeneratorId.Value > 0)
                        {
                            var leadGenerator = await db.CE_Sector
                                .FirstOrDefaultAsync(s => s.SectorID == contract.LeadGeneratorId.Value);
                            departmentFields["LeadGeneratorName"] = leadGenerator?.Name ?? "N/A";
                        }
                        else
                        {
                            departmentFields["LeadGeneratorName"] = "N/A";
                        }

                        // Populate Referral Partner
                        if (contract.ReferralPartnerId.HasValue && contract.ReferralPartnerId.Value > 0)
                        {
                            var referralPartner = await db.CE_Sector
                                .FirstOrDefaultAsync(s => s.SectorID == contract.ReferralPartnerId.Value);
                            departmentFields["ReferralPartnerName"] = referralPartner?.Name ?? "N/A";
                        }
                        else
                        {
                            departmentFields["ReferralPartnerName"] = "N/A";
                        }

                        // Populate Sub Referral Partner
                        if (contract.SubReferralPartnerId.HasValue && contract.SubReferralPartnerId.Value > 0)
                        {
                            var subReferralPartner = await db.CE_SubReferral
                                .FirstOrDefaultAsync(sr => sr.SubReferralID == contract.SubReferralPartnerId.Value);
                            departmentFields["SubReferralPartnerName"] = subReferralPartner?.SubReferralPartnerName ?? "N/A";
                        }
                        else
                        {
                            departmentFields["SubReferralPartnerName"] = "N/A";
                        }
                        break;

                    case "INTRODUCERS":
                        // Populate Introducers
                        if (contract.IntroducerId.HasValue && contract.IntroducerId.Value > 0)
                        {
                            var introducer = await db.CE_Sector
                                .FirstOrDefaultAsync(s => s.SectorID == contract.IntroducerId.Value);
                            departmentFields["IntroducerName"] = introducer?.Name ?? "N/A";
                        }
                        else
                        {
                            departmentFields["IntroducerName"] = "N/A";
                        }

                        // Populate Sub Introducers
                        if (contract.SubIntroducerId.HasValue && contract.SubIntroducerId.Value > 0)
                        {
                            var subIntroducer = await db.CE_SubIntroducer
                                .FirstOrDefaultAsync(si => si.SubIntroducerID == contract.SubIntroducerId.Value);
                            departmentFields["SubIntroducerName"] = subIntroducer?.SubIntroducerName ?? "N/A";
                        }
                        else
                        {
                            departmentFields["SubIntroducerName"] = "N/A";
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - this is not critical functionality
                System.Diagnostics.Debug.WriteLine($"PopulateDepartmentFieldsForElectric failed: {ex}");
            }
        }

        /// <summary>
        /// Populate department-based fields for Gas contracts
        /// </summary>
        /// <param name="contract">The gas contract</param>
        /// <param name="departmentFields">Dictionary to populate with field values</param>
        /// <param name="db">Database context</param>
        /// <returns>Task representing the async operation</returns>
        public static async Task PopulateDepartmentFieldsForGas(CE_GasContracts contract, System.Collections.Generic.Dictionary<string, string> departmentFields, ApplicationDBContext db)
        {
            try
            {
                if (contract == null) return;

                string department = contract.Department;
                if (string.IsNullOrWhiteSpace(department)) return;

                switch (department.ToUpper())
                {
                    case "BROKERS":
                        // Populate Brokerage Staff
                        if (contract.BrokerageStaffId.HasValue && contract.BrokerageStaffId.Value > 0)
                        {
                            var brokerageStaff = await db.CE_BrokerageStaff
                                .FirstOrDefaultAsync(bs => bs.BrokerageStaffID == contract.BrokerageStaffId.Value);
                            departmentFields["BrokerageStaffName"] = brokerageStaff?.BrokerageStaffName ?? "N/A";
                        }
                        else
                        {
                            departmentFields["BrokerageStaffName"] = "N/A";
                        }

                        // Populate Sub Brokerage
                        if (contract.SubBrokerageId.HasValue && contract.SubBrokerageId.Value > 0)
                        {
                            var subBrokerage = await db.CE_SubBrokerage
                                .FirstOrDefaultAsync(sb => sb.SubBrokerageID == contract.SubBrokerageId.Value);
                            departmentFields["SubBrokerageName"] = subBrokerage?.SubBrokerageName ?? "N/A";
                        }
                        else
                        {
                            departmentFields["SubBrokerageName"] = "N/A";
                        }
                        break;

                    case "IN HOUSE":
                        // Populate Closer
                        if (contract.CloserId.HasValue && contract.CloserId.Value > 0)
                        {
                            var closer = await db.CE_Sector
                                .FirstOrDefaultAsync(s => s.SectorID == contract.CloserId.Value);
                            departmentFields["CloserName"] = closer?.Name ?? "N/A";
                        }
                        else
                        {
                            departmentFields["CloserName"] = "N/A";
                        }

                        // Populate Lead Generator
                        if (contract.LeadGeneratorId.HasValue && contract.LeadGeneratorId.Value > 0)
                        {
                            var leadGenerator = await db.CE_Sector
                                .FirstOrDefaultAsync(s => s.SectorID == contract.LeadGeneratorId.Value);
                            departmentFields["LeadGeneratorName"] = leadGenerator?.Name ?? "N/A";
                        }
                        else
                        {
                            departmentFields["LeadGeneratorName"] = "N/A";
                        }

                        // Populate Referral Partner
                        if (contract.ReferralPartnerId.HasValue && contract.ReferralPartnerId.Value > 0)
                        {
                            var referralPartner = await db.CE_Sector
                                .FirstOrDefaultAsync(s => s.SectorID == contract.ReferralPartnerId.Value);
                            departmentFields["ReferralPartnerName"] = referralPartner?.Name ?? "N/A";
                        }
                        else
                        {
                            departmentFields["ReferralPartnerName"] = "N/A";
                        }

                        // Populate Sub Referral Partner
                        if (contract.SubReferralPartnerId.HasValue && contract.SubReferralPartnerId.Value > 0)
                        {
                            var subReferralPartner = await db.CE_SubReferral
                                .FirstOrDefaultAsync(sr => sr.SubReferralID == contract.SubReferralPartnerId.Value);
                            departmentFields["SubReferralPartnerName"] = subReferralPartner?.SubReferralPartnerName ?? "N/A";
                        }
                        else
                        {
                            departmentFields["SubReferralPartnerName"] = "N/A";
                        }
                        break;

                    case "INTRODUCERS":
                        // Populate Introducers
                        if (contract.IntroducerId.HasValue && contract.IntroducerId.Value > 0)
                        {
                            var introducer = await db.CE_Sector
                                .FirstOrDefaultAsync(s => s.SectorID == contract.IntroducerId.Value);
                            departmentFields["IntroducerName"] = introducer?.Name ?? "N/A";
                        }
                        else
                        {
                            departmentFields["IntroducerName"] = "N/A";
                        }

                        // Populate Sub Introducers
                        if (contract.SubIntroducerId.HasValue && contract.SubIntroducerId.Value > 0)
                        {
                            var subIntroducer = await db.CE_SubIntroducer
                                .FirstOrDefaultAsync(si => si.SubIntroducerID == contract.SubIntroducerId.Value);
                            departmentFields["SubIntroducerName"] = subIntroducer?.SubIntroducerName ?? "N/A";
                        }
                        else
                        {
                            departmentFields["SubIntroducerName"] = "N/A";
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - this is not critical functionality
                System.Diagnostics.Debug.WriteLine($"PopulateDepartmentFieldsForGas failed: {ex}");
            }
        }
    }
}
