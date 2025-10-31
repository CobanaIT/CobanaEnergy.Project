using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Extensions;
using CobanaEnergy.Project.Filters;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.Accounts.CalendarDashboard;
using CobanaEnergy.Project.Service;
using Logic;
using Logic.ResponseModel.Helper;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using CobanaEnergy.Project.Helpers;

namespace CobanaEnergy.Project.Controllers.Accounts.CalendarDashboard
{
    [Authorize(Roles = "Accounts,Controls")]
    public class CalendarDashboardController : BaseController
    {
        private readonly ApplicationDBContext db;

        public CalendarDashboardController(ApplicationDBContext _db)
        {
            db = _db;
        }

        [HttpGet]
        public ActionResult Index()
        {
            return View("~/Views/Accounts/CalendarDashboard/CalendarDashboard.cshtml");
        }


        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> GetCalendarContracts()
        {
            try
            {
                var dataTableRequest = DataTableHelperService.ParseDataTableRequest(Request.Form);
                string selectedDate = Request.Form["SelectedDate"];

                var query = db.CE_CommissionAndReconciliation
                    .Where(r => !string.IsNullOrEmpty(r.CommissionFollowUpDate));

                if (!string.IsNullOrWhiteSpace(selectedDate))
                {
                    query = query.Where(r => r.CommissionFollowUpDate == selectedDate);
                }

                int totalRecords = await query.CountAsync();

                if (!string.IsNullOrWhiteSpace(dataTableRequest.SearchValue))
                {
                    var matchingElectricIds = db.CE_ElectricContracts
                        .Where(c => c.InputDate.Contains(dataTableRequest.SearchValue))
                        .Select(c => c.EId);

                    var matchingGasIds = db.CE_GasContracts
                        .Where(c => c.InputDate.Contains(dataTableRequest.SearchValue))
                        .Select(c => c.EId);

                    query = query.Where(r =>
                        r.SupplierCobanaInvoiceNotes.Contains(dataTableRequest.SearchValue) ||
                        r.EId.Contains(dataTableRequest.SearchValue) ||
                        matchingElectricIds.Contains(r.EId) ||
                        matchingGasIds.Contains(r.EId) ||
                        db.CE_ElectricContracts.Any(c => c.EId == r.EId && c.ContractNotes.Contains(dataTableRequest.SearchValue)) ||
                        db.CE_GasContracts.Any(c => c.EId == r.EId && c.ContractNotes.Contains(dataTableRequest.SearchValue)) ||
                        db.CE_ContractStatuses.Any(cs => cs.EId == r.EId && cs.PaymentStatus.Contains(dataTableRequest.SearchValue)) ||
                        db.CE_ContractStatuses.Any(cs => cs.EId == r.EId && cs.ContractStatus.Contains(dataTableRequest.SearchValue)) ||
                        r.CommissionFollowUpDate.Contains(dataTableRequest.SearchValue) ||
                        r.contractType.Contains(dataTableRequest.SearchValue));
                }

                int filteredRecords = await query.CountAsync();

                var followUpRecords = await query
                    .OrderBy(r => r.CommissionFollowUpDate)
                    .ToListAsync();

                var contracts = new List<CalendarRowViewModel>();

                foreach (var record in followUpRecords)
                {
                    if (record.contractType == "Electric")
                    {
                        var electricContract = await db.CE_ElectricContracts
                            .FirstOrDefaultAsync(c => c.EId == record.EId);

                        if (electricContract != null)
                        {
                            var status = await db.CE_ContractStatuses
                                .FirstOrDefaultAsync(cs => cs.EId == electricContract.EId && cs.Type == "Electric");

                            contracts.Add(new CalendarRowViewModel
                            {
                                EId = electricContract.EId,
                                InputDate = ParserHelper.FormatDateForDisplay(electricContract.InputDate),
                                SortableDate = ParserHelper.ParseDateForSorting(electricContract.InputDate),
                                PaymentStatus = status?.PaymentStatus ?? "N/A",
                                ContractStatus = status?.ContractStatus ?? "N/A",
                                PreSalesNotes = electricContract.ContractNotes ?? "N/A",
                                SupplierCobanaInvoiceNotes = record.SupplierCobanaInvoiceNotes ?? "N/A",
                                Type = "Electric"
                            });
                        }
                    }
                    else if (record.contractType == "Gas")
                    {
                        var gasContract = await db.CE_GasContracts
                            .FirstOrDefaultAsync(c => c.EId == record.EId);

                        if (gasContract != null)
                        {
                            var status = await db.CE_ContractStatuses
                                .FirstOrDefaultAsync(cs => cs.EId == gasContract.EId && cs.Type == "Gas");

                            contracts.Add(new CalendarRowViewModel
                            {
                                EId = gasContract.EId,
                                InputDate = ParserHelper.FormatDateForDisplay(gasContract.InputDate),
                                SortableDate = ParserHelper.ParseDateForSorting(gasContract.InputDate),
                                PaymentStatus = status?.PaymentStatus ?? "N/A",
                                ContractStatus = status?.ContractStatus ?? "N/A",
                                PreSalesNotes = gasContract.ContractNotes ?? "N/A",
                                SupplierCobanaInvoiceNotes = record.SupplierCobanaInvoiceNotes ?? "N/A",
                                Type = "Gas"
                            });
                        }
                    }
                }

                // Apply DataTable sorting and pagination to the processed contracts
                var columnMappings = GetColumnMappings();
                var columnNames = GetColumnNames(); // Get column names in correct order
                var queryableContracts = contracts.AsQueryable();
                var sortedContracts = queryableContracts.ApplyDataTableSorting(dataTableRequest, columnMappings, columnNames, "SortableDate", true, false);
                
                var paginatedContracts = sortedContracts
                    .Skip(dataTableRequest.Start)
                    .Take(dataTableRequest.Length)
                    .ToList();

                return Json(new DataTableHelperService.DataTableResponse<CalendarRowViewModel>
                {
                    draw = dataTableRequest.Draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = filteredRecords,
                    data = paginatedContracts
                });
            }
            catch (Exception ex)
            {
                Logger.Log("GetCalendarContracts: " + ex);
                return JsonResponse.Fail("Error fetching calendar contracts.");
            }
        }

        private Dictionary<string, Func<IQueryable<CalendarRowViewModel>, bool, IQueryable<CalendarRowViewModel>>> GetColumnMappings()
        {
            return new Dictionary<string, Func<IQueryable<CalendarRowViewModel>, bool, IQueryable<CalendarRowViewModel>>>
            {
                ["InputDate"] = (query, ascending) => ascending ? query.OrderBy(x => x.SortableDate) : query.OrderByDescending(x => x.SortableDate),
                ["PaymentStatus"] = (query, ascending) => ascending ? query.OrderBy(x => x.PaymentStatus) : query.OrderByDescending(x => x.PaymentStatus),
                ["ContractStatus"] = (query, ascending) => ascending ? query.OrderBy(x => x.ContractStatus) : query.OrderByDescending(x => x.ContractStatus),
                ["PreSalesNotes"] = (query, ascending) => ascending ? query.OrderBy(x => x.PreSalesNotes) : query.OrderByDescending(x => x.PreSalesNotes),
                ["SupplierCobanaInvoiceNotes"] = (query, ascending) => ascending ? query.OrderBy(x => x.SupplierCobanaInvoiceNotes) : query.OrderByDescending(x => x.SupplierCobanaInvoiceNotes)
            };
        }

        /// <summary>
        /// Get column names in the exact order they appear in the DataTable
        /// </summary>
        private string[] GetColumnNames()
        {
            return new[] { 
                "InputDate", "PaymentStatus", "ContractStatus", "PreSalesNotes", "SupplierCobanaInvoiceNotes" 
            };
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> GetFollowUpDates()
        {
            try
            {
                var dateList = await db.CE_CommissionAndReconciliation
                    .Where(r => !string.IsNullOrEmpty(r.CommissionFollowUpDate))
                    .Select(r => r.CommissionFollowUpDate.Trim())
                    .Distinct()
                    .ToListAsync();

                var formatted = dateList
                    .Select(d =>
                    {
                        DateTime parsed;
                        return DateTime.TryParse(d, out parsed)
                            ? parsed.ToString("yyyy-MM-dd")
                            : null;
                    })
                    .Where(x => x != null)
                    .ToList();

                return JsonResponse.Ok(formatted); // ✅ Final Fix
            }
            catch (Exception ex)
            {
                Logger.Log("GetFollowUpDates error: " + ex);
                return JsonResponse.Fail("Could not fetch follow-up dates.");
            }
        }

    }
}
