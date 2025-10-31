using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Accounts.InvoiceSupplierDashboard
{
    public static class ContractStatusHelper
    {
        public static readonly HashSet<string> ExcludedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Objection Closed|Never Live - Resolved",
            "Contract Ended - Ag Lost|Resolve",
            "Contract Ended - Not Renewed|Resolve",
            "Contract Ended - Renewed|Resolve",
            "Lost|Resolve",
            "Credit Failed|Never Live - Resolved",
            "Rejected|Never Live - Resolved",
            "Dead - No Action Required|Never Live - Resolved",
            "Dead - Credit Failed|Never Live - Resolved",
            "Dead - Valid Contract in Place|Never Live - Resolved",
            "Dead - Duplicate Submission|Never Live - Resolved",
            "Dead - Due to Objections|Never Live - Resolved"
        };

        public static bool IsExcluded(string contractStatus, string paymentStatus)
        {
            string key = $"{contractStatus ?? ""}|{paymentStatus ?? ""}";
            return ExcludedKeys.Contains(key);
        }


        public static readonly Dictionary<string, string> AllStatuses = new Dictionary<string, string>
            {
                { "Pending", "#B0B0B0" },                   
                { "Processing_Present Month", "#FFF48F" },  
                { "Processing_Future Months", "#FFD45B" },  
                { "Objection", "#FF6B6B" },                 
                { "Objection Closed", "#B9A7D6" },          
                { "Reapplied", "#8FC1C9" },                 
                { "New Lives", "#8DD28B" },                 
                { "Live", "#4FF574" },                      
                { "Renewal Window", "#F5A45B" },            
                { "Renewal Window - Ag Lost", "#F8BE7E" },  
                { "Renewed", "#7DC87E" },                   
                { "Possible Loss", "#6ED9E8" },             
                { "Lost", "#3C78D8" },                      
                { "Credit Failed", "#A6A6A6" },             
                { "Rejected", "#BDBDBD" },                  
                { "To Be Resolved - Cobana", "#FFD86E" },   
                { "Waiting Agent", "#66B3FF" },             
                { "Waiting Supplier", "#A4C2F4" }           
            };

    }
}