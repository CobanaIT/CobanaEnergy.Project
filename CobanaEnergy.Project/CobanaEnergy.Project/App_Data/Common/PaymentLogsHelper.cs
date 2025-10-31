using CobanaEnergy.Project.Helpers;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.Accounts.MasterDashboard.AccountMasterDashboard;
using CobanaEnergy.Project.Models.Accounts.SuppliersModels;
using CobanaEnergy.Project.Models.Accounts.SuppliersModels.BGB.DBModel;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace CobanaEnergy.Project.Common
{
    public static class PaymentLogsHelper
    {
        public static void InsertPaymentAndNotesLogs(
        ApplicationDBContext db, PaymentAndNotesLogsViewModel model)
        {
            if (!string.IsNullOrEmpty(model.PaymentStatus) || !string.IsNullOrEmpty(model.CobanaInvoiceNotes))
            {
                var log = new CE_PaymentAndNoteLogs
                {
                    EId = model.EId,
                    PaymentStatus = model.PaymentStatus,
                    CobanaInvoiceNotes = model.CobanaInvoiceNotes,
                    Username = model.Username ?? "Unknown User",
                    contracttype = model.ContractType,
                    Dashboard = model.Dashboard,
                    CreatedAt = DateTime.Now,
                };

                db.CE_PaymentAndNoteLogs.Add(log);
            }
        }

        public static async Task<List<(string Label, int Count)>> GetCounterAsync(
                 List<(string Label, string Status)> statuses,
                 ApplicationDBContext dbContext)
        {
            var statusKeys = statuses.Select(s => s.Status).ToList();

            var dbCounts = await dbContext.CE_ContractStatuses
                .Where(cs => statusKeys.Contains(cs.PaymentStatus))
                .GroupBy(cs => cs.PaymentStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            return statuses
                .Select(s => (s.Label, dbCounts.FirstOrDefault(dc => dc.Status == s.Status)?.Count ?? 0))
                .ToList();
        }
    }

    public static class HelperUtility
    {
        public static List<(string Label, string Status)> GetStatuses()
        {
            return new List<(string, string)>
        {
            ("Contracts Awaiting Invoice", "Awaiting Invoice"),
            ("Contracts Awaiting 1st Reconciliation", "Awaiting 1st Reconciliation"),
            ("Contracts Awaiting 2nd Initial", "Awaiting 2nd initial"),
            ("Contracts Awaiting 2nd Reconciliation", "Awaiting 2nd Reconciliation"),
            ("Contracts Awaiting 3rd Initial", "Awaiting 3rd initial"),
            ("Contracts Awaiting 3rd Reconciliation", "Awaiting 3rd Reconciliation"),
            ("Contracts Awaiting 4th Initial", "Awaiting 4th initial"),
            ("Contracts Awaiting 4th Reconciliation", "Awaiting 4th Reconciliation"),
            ("Contracts Awaiting 5th Initial", "Awaiting 5th initial"),
            ("Contracts Awaiting Final Reconciliation", "Awaiting Final Reconciliation")
        };
        }

        public static List<(string Label, string Status)> GetContractStatus()
        {
            return SupportedSuppliers._statusWaitDays
                    .Where(x => x.Key.Contains("Month Payment"))
                    .OrderBy(x => x.Value) // Sort by wait days, so statuses come in sequence
                    .Select(kvp => ($"Contracts {kvp.Key}", kvp.Key))
                    .ToList();
        }

        public static List<(string Label, string Status)> GetQuarterlyContractStatus()
        {
            return SupportedSuppliers._statusWaitDays
                    .Where(x => x.Key.Contains("Qtr") || x.Key.Contains("EDF"))
                    .OrderBy(x => x.Value) // Sort by wait days, so statuses come in sequence
                    .Select(kvp => ($"Contracts {kvp.Key}", kvp.Key))
                    .ToList();
        }


        public static DateTime GetEffectiveCED(ContractViewModel x, CE_CommissionAndReconciliation cr)
        {
            // Step 1: If CED is already filled in reconciliation
            if (!string.IsNullOrWhiteSpace(cr?.CED))
                return ParserHelper.ParseDateForSorting(cr.CED);

            // Step 2: Determine which StartDate to use
            string startDateString = null;

            if (!string.IsNullOrWhiteSpace(cr?.StartDate))
                startDateString = cr.StartDate;
            else
                startDateString = x.StartDate;

            // Step 3: Apply CED formula if valid start date + duration exist
            if (DateTime.TryParse(startDateString, out var startDate) &&
                int.TryParse(x.Duration?.ToString(), out var durYears))
            {
                var calculated = startDate.AddYears(durYears).AddDays(-1);
                return calculated;
            }

            // Step 4: Fallback
            return DateTime.MinValue;
        }


    }


    
}