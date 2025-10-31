using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Service.HelperUtilityService
{
    public static class HelperService
    {
        private static readonly List<string> EdfQueries = new List<string>
        {
            "Cancellation requests",
            "Contract end date",
            "Contract status updates",
            "COT Application",
            "Credit checks",
            "Customer Billing Query",
            "Customer Disputed reads",
            "Customer Meter readings",
            "Customer Payment/ DD set up & DD query",
            "Customer Smart metering",
            "Erroneous Transfer",
            "Letter of authority",
            "Objection Queries (Possible Loss)",
            "Objection Resolved (Reapplied)",
            "Objections",
            "Portal access requests",
            "Portal credit appeals",
            "Portal submission failures",
            "Portal submission queries",
            "Start Date changes",
            "Sub-broker approval",
            "Supported meter checks"
        };

        private static readonly List<string> BgbQueries = new List<string>
            {
                "Cancellation requests",
                "Contract status updates",
                "COT Application",
                "Customer Billing Query",
                "Customer Disputed reads",
                "Customer Meter readings",
                "Customer Payment/ DD set up & DD query",
                "Erroneous Transfer",
                "Letter of authority",
                "Objection Queries (Possible Loss)",
                "Objection Resolved (Reapplied)",
                "Objections",
                "Portal access requests",
                "Portal submission failures",
                "Portal submission queries",
                "Start Date changes",
                "Sub-broker approval",
                "Supported meter checks"
            };

        public static readonly Dictionary<string, List<string>> SupplierQueryTypes = new Dictionary<string, List<string>>
        {
            { "Others", new List<string> { "N/A" } },

            // BG Lite
            { "BG Lite", BgbQueries},
            { "British Gas Business", BgbQueries},

            // EDF
            { "EDF I&C", EdfQueries },
            { "EDF SME", EdfQueries },

            // Scottish Power
            { "Scottish Power", new List<string>
            {
                "Cancellation requests",
                "Contract status updates",
                "COT Application",
                "Customer Billing Query",
                "Customer Disputed reads",
                "Customer Meter readings",
                "Customer Payment/ DD set up & DD query",
                "Erroneous Transfer",
                "Handkey submissions",
                "Objection Queries (Possible Loss)",
                "Objection Resolved (Reapplied)",
                "Objections",
                "Portal access requests",
                "Portal credit appeals",
                "Sub-broker approval"
            }},

            // Smartest Energy
            { "Smartest Energy", new List<string>
            {
                "Cancellation requests",
                "Contract status updates",
                "COT Application",
                "Customer Billing Query",
                "Customer Disputed reads",
                "Customer Meter readings",
                "Customer Payment/ DD set up & DD query",
                "Erroneous Transfer",
                "Letter of authority",
                "Objection Queries (Possible Loss)",
                "Objection Resolved (Reapplied)",
                "Objections",
                "Portal access requests",
                "Portal submission failures",
                "Portal submission queries",
                "Start Date changes",
                "Sub-broker approval",
                "Supported meter checks"
            }},

            // Corona
            { "Corona", new List<string>
            {
                "Cancellation requests",
                "Contract end date",
                "Contract status updates",
                "COT Application",
                "Customer Billing Query",
                "Customer Disputed reads",
                "Customer Meter readings",
                "Customer Payment/ DD set up & DD query",
                "Erroneous Transfer",
                "Letter of authority",
                "Objection Queries (Possible Loss)",
                "Objection Resolved (Reapplied)",
                "Objections",
                "Portal access requests",
                "Portal credit appeals",
                "Portal submission queries",
                "Start Date changes",
                "Sub-broker approval",
                "Supported meter checks"
            }},

            // Crown Gas & Power
            { "Crown Gas & Power", new List<string>
            {
                "Cancellation requests",
                "Contract end date",
                "Contract status updates",
                "COT Application",
                "Customer Billing Query",
                "Customer Disputed reads",
                "Customer Meter readings",
                "Customer Payment/ DD set up & DD query",
                "Erroneous Transfer",
                "Letter of authority",
                "Objection Queries (Possible Loss)",
                "Objection Resolved (Reapplied)",
                "Objections",
                "Portal access requests",
                "Portal credit appeals",
                "Portal submission queries",
                "Start Date changes",
                "Sub-broker approval",
                "Supported meter checks"
            }},

            // SSE
            { "SSE", new List<string>
            {
                "Cancellation requests",
                "Contract end date",
                "Contract status updates",
                "COT Application",
                "Customer Billing Query",
                "Customer Disputed reads",
                "Customer Meter readings",
                "Customer Payment/ DD set up & DD query",
                "Erroneous Transfer",
                "Letter of authority",
                "Objection Queries (Possible Loss)",
                "Objection Resolved (Reapplied)",
                "Objections",
                "Portal access requests",
                "Portal credit appeals",
                "Portal submission queries",
                "Start Date changes",
                "Sub-broker approval",
                "Supported meter checks"
            }},

            // Total Gas & Power
            { "Total Gas & Power", new List<string>
            {
                "Cancellation requests",
                "Contract end date",
                "Contract status updates",
                "COT Application",
                "Customer Billing Query",
                "Customer Disputed reads",
                "Customer Meter readings",
                "Customer Payment/ DD set up & DD query",
                "Erroneous Transfer",
                "Letter of authority",
                "Objection Queries (Possible Loss)",
                "Objection Resolved (Reapplied)",
                "Objections",
                "Portal access requests",
                "Portal credit appeals",
                "Portal submission queries",
                "Start Date changes",
                "Sub-broker approval",
                "Supported meter checks"
            }}
        };
    }
}