using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Extensions;
using CobanaEnergy.Project.Filters;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.PreSales;
using CobanaEnergy.Project.Service;
using Logic;
using Logic.ResponseModel.Helper;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Controllers.PreSales
{
    [Authorize(Roles = "Pre-sales")]
    public class PreSalesCalendarController : BaseController
    {
        private readonly ApplicationDBContext db;

        public PreSalesCalendarController(ApplicationDBContext _db)
        {
            db = _db;
        }

        [HttpGet]
        public ActionResult Index()
        {
            return View("~/Views/PreSales/PreSalesCalendar.cshtml");
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> GetPreSalesCalendarContracts()
        {
            try
            {
                var dataTableRequest = DataTableHelperService.ParseDataTableRequest(Request.Form);
                string selectedDate = Request.Form["SelectedDate"];

                // Get Electric contracts with PreSalesFollowUpDate
                var electricQuery = db.CE_ElectricContracts
                    .Where(c => c.PreSalesFollowUpDate != null);

                // Get Gas contracts with PreSalesFollowUpDate
                var gasQuery = db.CE_GasContracts
                    .Where(c => c.PreSalesFollowUpDate != null);

                if (!string.IsNullOrWhiteSpace(selectedDate))
                {
                    DateTime selectedDateTime;
                    if (DateTime.TryParse(selectedDate, out selectedDateTime))
                    {
                        electricQuery = electricQuery.Where(c => DbFunctions.TruncateTime(c.PreSalesFollowUpDate.Value) == selectedDateTime.Date);
                        gasQuery = gasQuery.Where(c => DbFunctions.TruncateTime(c.PreSalesFollowUpDate.Value) == selectedDateTime.Date);
                    }
                }

                // Apply search filters
                if (!string.IsNullOrWhiteSpace(dataTableRequest.SearchValue))
                {
                    electricQuery = electricQuery.Where(c =>
                        c.EId.Contains(dataTableRequest.SearchValue) ||
                        c.MPAN.Contains(dataTableRequest.SearchValue) ||
                        c.PostCode.Contains(dataTableRequest.SearchValue) ||
                        c.BusinessName.Contains(dataTableRequest.SearchValue) ||
                        c.CurrentSupplier.Contains(dataTableRequest.SearchValue) ||
                        c.ContractNotes.Contains(dataTableRequest.SearchValue) ||
                        // Add agent search by joining with lookup tables
                        (c.Department == "In House" && c.CloserId.HasValue &&
                         db.CE_Sector.Any(s => s.SectorID == c.CloserId && s.SectorType == "closer" && s.Name.Contains(dataTableRequest.SearchValue))) ||
                        (c.Department == "Brokers" && c.BrokerageStaffId.HasValue &&
                         db.CE_BrokerageStaff.Any(bs => bs.BrokerageStaffID == c.BrokerageStaffId && bs.BrokerageStaffName.Contains(dataTableRequest.SearchValue))) ||
                        (c.Department == "Introducers" && c.SubIntroducerId.HasValue &&
                         db.CE_SubIntroducer.Any(si => si.SubIntroducerID == c.SubIntroducerId && si.SubIntroducerName.Contains(dataTableRequest.SearchValue))));
                }

                // Get total counts
                int electricTotal = await electricQuery.CountAsync();
                int gasTotal = await gasQuery.CountAsync();
                int totalRecords = electricTotal + gasTotal;

                // Get all results for proper sorting and pagination
                var electricContracts = await electricQuery
                    .OrderBy(c => c.PreSalesFollowUpDate)
                    .ToListAsync();

                // Process electric contracts with agent determination
                var processedElectricContracts = electricContracts.Select(c =>
                {
                    string agentName = "-";
                    
                    if (c.Department == "In House" && c.CloserId.HasValue)
                    {
                        var closer = db.CE_Sector
                            .Where(s => s.SectorID == c.CloserId && s.SectorType == "closer")
                            .Select(s => s.Name)
                            .FirstOrDefault();
                        agentName = closer ?? "-";
                    }
                    else if (c.Department == "Brokers" && c.BrokerageStaffId.HasValue)
                    {
                        var brokerageStaff = db.CE_BrokerageStaff
                            .Where(bs => bs.BrokerageStaffID == c.BrokerageStaffId)
                            .Select(bs => bs.BrokerageStaffName)
                            .FirstOrDefault();
                        agentName = brokerageStaff ?? "-";
                    }
                    else if (c.Department == "Introducers" && c.SubIntroducerId.HasValue)
                    {
                        var subIntroducer = db.CE_SubIntroducer
                            .Where(si => si.SubIntroducerID == c.SubIntroducerId)
                            .Select(si => si.SubIntroducerName)
                            .FirstOrDefault();
                        agentName = subIntroducer ?? "-";
                    }

                    return new PreSalesCalendarRowViewModel
                    {
                        EId = c.EId,
                        Agent = agentName,
                        MPAN = c.MPAN,
                        MPRN = "",
                        PostCode = c.PostCode,
                        BusinessName = c.BusinessName,
                        Supplier = c.CurrentSupplier,
                        Type = "Electric",
                        PreSalesFollowUpDate = c.PreSalesFollowUpDate
                    };
                }).ToList();

                // If we need more records and haven't reached the end of electric contracts
                var remainingLength = dataTableRequest.Length - electricContracts.Count;
                if (remainingLength > 0)
                {
                    var gasContracts = await gasQuery
                        .OrderBy(c => c.PreSalesFollowUpDate)
                        .Skip(Math.Max(0, dataTableRequest.Start - electricTotal))
                        .Take(remainingLength)
                        .ToListAsync();

                    // Process gas contracts with agent determination
                    var processedGasContracts = gasContracts.Select(c =>
                    {
                        string agentName = "-";
                        
                        if (c.Department == "In House" && c.CloserId.HasValue)
                        {
                            var closer = db.CE_Sector
                                .Where(s => s.SectorID == c.CloserId && s.SectorType == "closer")
                                .Select(s => s.Name)
                                .FirstOrDefault();
                            agentName = closer ?? "-";
                        }
                        else if (c.Department == "Brokers" && c.BrokerageStaffId.HasValue)
                        {
                            var brokerageStaff = db.CE_BrokerageStaff
                                .Where(bs => bs.BrokerageStaffID == c.BrokerageStaffId)
                                .Select(bs => bs.BrokerageStaffName)
                                .FirstOrDefault();
                            agentName = brokerageStaff ?? "-";
                        }
                        else if (c.Department == "Introducers" && c.SubIntroducerId.HasValue)
                        {
                            var subIntroducer = db.CE_SubIntroducer
                                .Where(si => si.SubIntroducerID == c.SubIntroducerId)
                                .Select(si => si.SubIntroducerName)
                                .FirstOrDefault();
                            agentName = subIntroducer ?? "-";
                        }

                        return new PreSalesCalendarRowViewModel
                        {
                            EId = c.EId,
                            Agent = agentName,
                            MPAN = "",
                            MPRN = c.MPRN,
                            PostCode = c.PostCode,
                            BusinessName = c.BusinessName,
                            Supplier = c.CurrentSupplier,
                            Type = "Gas",
                            PreSalesFollowUpDate = c.PreSalesFollowUpDate
                        };
                    }).ToList();

                    processedElectricContracts.AddRange(processedGasContracts);
                }

                // Apply DataTable sorting and pagination to the processed contracts
                var columnMappings = GetColumnMappings();
                var columnNames = GetColumnNames(); // Get column names in correct order
                var queryableContracts = processedElectricContracts.AsQueryable();
                var sortedContracts = queryableContracts.ApplyDataTableSorting(dataTableRequest, columnMappings, columnNames, "Agent", true);
                
                var paginatedContracts = sortedContracts
                    .Skip(dataTableRequest.Start)
                    .Take(dataTableRequest.Length)
                    .ToList();

                return Json(new DataTableHelperService.DataTableResponse<PreSalesCalendarRowViewModel>
                {
                    draw = dataTableRequest.Draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalRecords,
                    data = paginatedContracts
                });
            }
            catch (Exception ex)
            {
                Logger.Log("GetPreSalesCalendarContracts: " + ex);
                return JsonResponse.Fail("Error fetching pre-sales calendar contracts.");
            }
        }

        private Dictionary<string, Func<IQueryable<PreSalesCalendarRowViewModel>, bool, IQueryable<PreSalesCalendarRowViewModel>>> GetColumnMappings()
        {
            return new Dictionary<string, Func<IQueryable<PreSalesCalendarRowViewModel>, bool, IQueryable<PreSalesCalendarRowViewModel>>>
            {
                // Column order: EId(0), Agent(1), MPAN(2), MPRN(3), PostCode(4), BusinessName(5), Supplier(6)
                ["Agent"] = (query, ascending) => ascending ? query.OrderBy(x => x.Agent) : query.OrderByDescending(x => x.Agent),
                ["MPAN"] = (query, ascending) => ascending ? query.OrderBy(x => x.MPAN) : query.OrderByDescending(x => x.MPAN),
                ["MPRN"] = (query, ascending) => ascending ? query.OrderBy(x => x.MPRN) : query.OrderByDescending(x => x.MPRN),
                ["PostCode"] = (query, ascending) => ascending ? query.OrderBy(x => x.PostCode) : query.OrderByDescending(x => x.PostCode),
                ["BusinessName"] = (query, ascending) => ascending ? query.OrderBy(x => x.BusinessName) : query.OrderByDescending(x => x.BusinessName),
                ["Supplier"] = (query, ascending) => ascending ? query.OrderBy(x => x.Supplier) : query.OrderByDescending(x => x.Supplier)
            };
        }

        /// <summary>
        /// Get column names in the exact order they appear in the DataTable
        /// </summary>
        private string[] GetColumnNames()
        {
            return new[] { 
                "EId", "Agent", "MPAN", "MPRN", "PostCode", "BusinessName", "Supplier" 
            };
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> GetPreSalesFollowUpDates()
        {
            try
            {
                // Get unique dates from Electric contracts
                var electricDates = await db.CE_ElectricContracts
                    .Where(c => c.PreSalesFollowUpDate != null)
                    .Select(c => c.PreSalesFollowUpDate.Value)
                    .Distinct()
                    .ToListAsync();

                // Get unique dates from Gas contracts
                var gasDates = await db.CE_GasContracts
                    .Where(c => c.PreSalesFollowUpDate != null)
                    .Select(c => c.PreSalesFollowUpDate.Value)
                    .Distinct()
                    .ToListAsync();

                // Combine and format dates
                var allDates = electricDates.Union(gasDates)
                    .Select(d => d.ToString("yyyy-MM-dd"))
                    .Distinct()
                    .OrderBy(d => d)
                    .ToList();

                return JsonResponse.Ok(allDates);
            }
            catch (Exception ex)
            {
                Logger.Log("GetPreSalesFollowUpDates error: " + ex);
                return JsonResponse.Fail("Could not fetch pre-sales follow-up dates.");
            }
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> GetContractNotes(string eId, string contractType)
        {
            try
            {
                var model = new EditContractNotesPopupViewModel
                {
                    EId = eId,
                    ContractType = contractType,
                    ContractNotes = "",
                    PreSalesFollowUpDate = "",
                    IsDualContract = false
                };

                if (contractType == "Electric")
                {
                    var electricContract = await db.CE_ElectricContracts
                        .Where(c => c.EId == eId)
                        .FirstOrDefaultAsync();

                    if (electricContract != null)
                    {
                        model.ContractNotes = electricContract.ContractNotes ?? "";
                        model.PreSalesFollowUpDate = electricContract.PreSalesFollowUpDate?.ToString("yyyy-MM-dd") ?? "";

                        //Check if it is also a dual contract
                        var gasContract = await db.CE_GasContracts.Where(c => c.EId == eId).FirstOrDefaultAsync();
                        model.IsDualContract = gasContract != null;
                    }

                    
                }
                else if (contractType == "Gas")
                {
                    var gasContract = await db.CE_GasContracts
                        .Where(c => c.EId == eId)
                        .FirstOrDefaultAsync();

                    if (gasContract != null)
                    {
                        model.ContractNotes = gasContract.ContractNotes ?? "";
                        model.PreSalesFollowUpDate = gasContract.PreSalesFollowUpDate?.ToString("yyyy-MM-dd") ?? "";

                        //Check if it is also a dual contract
                        var electricalContract = await db.CE_ElectricContracts.Where(c => c.EId == eId).FirstOrDefaultAsync();
                        model.IsDualContract = electricalContract != null;
                    }
                }

                return JsonResponse.Ok(model);
            }
            catch (Exception ex)
            {
                Logger.Log("GetContractNotes error: " + ex);
                return JsonResponse.Fail("Could not fetch contract notes.");
            }
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> UpdateContractNotes(EditContractNotesPopupViewModel model)
        {
            try
            {
                if (model.ContractType == "Electric")
                {
                    var electricContract = await db.CE_ElectricContracts
                        .Where(c => c.EId == model.EId)
                        .FirstOrDefaultAsync();

                    if (electricContract != null)
                    {
                        electricContract.ContractNotes = model.ContractNotes;
                        electricContract.PreSalesFollowUpDate = DateTime.TryParse(model.PreSalesFollowUpDate, out DateTime presalesDate) ? presalesDate : (DateTime?)null;
                        await db.SaveChangesAsync();
                    }
                }
                else if (model.ContractType == "Gas")
                {
                    var gasContract = await db.CE_GasContracts
                        .Where(c => c.EId == model.EId)
                        .FirstOrDefaultAsync();

                    if (gasContract != null)
                    {
                        gasContract.ContractNotes = model.ContractNotes;
                        gasContract.PreSalesFollowUpDate = DateTime.TryParse(model.PreSalesFollowUpDate, out DateTime presalesDate) ? presalesDate : (DateTime?)null;
                        await db.SaveChangesAsync();
                    }
                }

                return JsonResponse.Ok("Contract notes and follow-up date updated successfully.");
            }
            catch (Exception ex)
            {
                Logger.Log("UpdateContractNotes error: " + ex);
                return JsonResponse.Fail("Could not update contract notes.");
            }
        }
    }
}

