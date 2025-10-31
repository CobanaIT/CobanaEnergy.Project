using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Extensions;
using CobanaEnergy.Project.Filters;
using CobanaEnergy.Project.Helpers;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.PreSales;
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
using System.Globalization;


namespace CobanaEnergy.Project.Controllers.PreSales
{
    [Authorize(Roles = "Pre-sales")]
    public class PreSalesMasterDashboardController : BaseController
    {
        private readonly ApplicationDBContext db;

        public PreSalesMasterDashboardController(ApplicationDBContext _db)
        {
            db = _db;
        }

        [HttpGet]
        public async Task<ActionResult> Index()
        {
            var suppliers = await db.CE_Supplier
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                })
                .ToListAsync();

            // Get contract status counts
            var statusCounts = await GetContractStatusCounts();

            var model = new PreSalesMasterDashboardViewModel
            {
                Suppliers = suppliers,
                SubmittedCount = statusCounts.SubmittedCount,
                RejectedCount = statusCounts.RejectedCount,
                PendingCount = statusCounts.PendingCount
            };

            return View("~/Views/PreSales/PreSalesMasterDashboard.cshtml", model);
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> GetPreSalesMasterContracts()
        {
            try
            {
                var dataTableRequest = DataTableHelperService.ParseDataTableRequest(Request.Form);
                string inputDateFrom = Request.Form["inputDateFrom"];
                string inputDateTo = Request.Form["inputDateTo"];
                string supplierId = Request.Form["supplierId"];

                // Parse date filters
                DateTime? fromDate = null;
                DateTime? toDate = null;
                
                if (!string.IsNullOrWhiteSpace(inputDateFrom) && DateTime.TryParse(inputDateFrom, out DateTime from))
                    fromDate = from.Date;
                
                if (!string.IsNullOrWhiteSpace(inputDateTo) && DateTime.TryParse(inputDateTo, out DateTime to))
                    toDate = to.Date.AddDays(1).AddTicks(-1); // End of day

                // Parse supplier filter
                long? supplierFilter = null;
                if (!string.IsNullOrWhiteSpace(supplierId) && long.TryParse(supplierId, out long supplierIdLong))
                    supplierFilter = supplierIdLong;

                // Build optimized queries with database-level filtering
                var electricQuery = BuildElectricQuery(supplierFilter, fromDate, toDate, dataTableRequest.SearchValue);
                var gasQuery = BuildGasQuery(supplierFilter, fromDate, toDate, dataTableRequest.SearchValue);

                // Get total counts for each table (for accurate pagination)
                var electricTotal = await electricQuery.CountAsync();
                var gasTotal = await gasQuery.CountAsync();

                // Calculate how many records to fetch from each table
                // We need to account for dual contracts, so fetch more records
                var fetchLimit = Math.Max(dataTableRequest.Length * 2, 1000); // Fetch at least 2x the page size or 1000 records

                // Get electric contracts with pagination
                var electricContracts = await electricQuery
                    .OrderByDescending(e => e.InputDate)
                    .Take(fetchLimit)
                    .ToListAsync();

                // Get gas contracts with pagination
                var gasContracts = await gasQuery
                    .OrderByDescending(g => g.InputDate)
                    .Take(fetchLimit)
                    .ToListAsync();


                // Process contracts to handle dual contracts efficiently
                var combinedContracts = await ProcessCombinedContractsOptimized(electricContracts, gasContracts);

                // Apply final search filter if not already applied at database level
                if (!string.IsNullOrWhiteSpace(dataTableRequest.SearchValue) && !IsDatabaseSearchSupported(dataTableRequest.SearchValue))
                {
                    combinedContracts = combinedContracts.Where(c =>
                        c.BusinessName.Contains(dataTableRequest.SearchValue) ||
                        c.CustomerName.Contains(dataTableRequest.SearchValue) ||
                        c.PostCode.Contains(dataTableRequest.SearchValue) ||
                        c.Email.Contains(dataTableRequest.SearchValue) ||
                        c.Supplier.Contains(dataTableRequest.SearchValue) ||
                        c.ContractNotes.Contains(dataTableRequest.SearchValue) ||
                        c.MPAN.Contains(dataTableRequest.SearchValue) ||
                        c.MPRN.Contains(dataTableRequest.SearchValue)).ToList();
                }

                // Get total count for filtered results
                int totalRecords = combinedContracts.Count;

                // Apply DataTable sorting and pagination
                var columnMappings = GetColumnMappings();
                var columnNames = GetColumnNames(); // Get column names in correct order
                var queryableContracts = combinedContracts.AsQueryable();
                var sortedContracts = queryableContracts.ApplyDataTableSorting(dataTableRequest, columnMappings, columnNames, "InputDate", false);
                
                var paginatedContracts = sortedContracts
                    .Skip(dataTableRequest.Start)
                    .Take(dataTableRequest.Length)
                    .ToList();

                return Json(new DataTableHelperService.DataTableResponse<PreSalesMasterDashboardRowViewModel>
                {
                    draw = dataTableRequest.Draw,
                    recordsTotal = electricTotal + gasTotal, // Total records in both tables
                    recordsFiltered = totalRecords, // Filtered count
                    data = paginatedContracts
                });
            }
            catch (Exception ex)
            {
                Logger.Log("GetPreSalesMasterContracts: " + ex);
                return JsonResponse.Fail("Error fetching pre-sales master contracts.");
            }
        }

        private Dictionary<string, Func<IQueryable<PreSalesMasterDashboardRowViewModel>, bool, IQueryable<PreSalesMasterDashboardRowViewModel>>> GetColumnMappings()
        {
            return new Dictionary<string, Func<IQueryable<PreSalesMasterDashboardRowViewModel>, bool, IQueryable<PreSalesMasterDashboardRowViewModel>>>
            {
                // Column order: Action(0), Agent(1), BusinessName(2), CustomerName(3), PostCode(4), InputDate(5), Duration(6), Uplift(7), InputEAC(8), Email(9), Supplier(10), ContractNotes(11), SortableDate(12)
                ["Agent"] = (query, ascending) => ascending ? query.OrderBy(x => x.Agent) : query.OrderByDescending(x => x.Agent),
                ["BusinessName"] = (query, ascending) => ascending ? query.OrderBy(x => x.BusinessName) : query.OrderByDescending(x => x.BusinessName),
                ["CustomerName"] = (query, ascending) => ascending ? query.OrderBy(x => x.CustomerName) : query.OrderByDescending(x => x.CustomerName),
                ["PostCode"] = (query, ascending) => ascending ? query.OrderBy(x => x.PostCode) : query.OrderByDescending(x => x.PostCode),
                ["InputDate"] = (query, ascending) => ascending ? query.OrderBy(x => x.SortableDate) : query.OrderByDescending(x => x.SortableDate),
                ["Duration"] = (query, ascending) => ascending ? query.OrderBy(x => x.Duration) : query.OrderByDescending(x => x.Duration),
                ["Uplift"] = (query, ascending) => ascending ? query.OrderBy(x => x.Uplift) : query.OrderByDescending(x => x.Uplift),
                ["InputEAC"] = (query, ascending) => ascending ? query.OrderBy(x => x.InputEAC) : query.OrderByDescending(x => x.InputEAC),
                ["Email"] = (query, ascending) => ascending ? query.OrderBy(x => x.Email) : query.OrderByDescending(x => x.Email),
                ["Supplier"] = (query, ascending) => ascending ? query.OrderBy(x => x.Supplier) : query.OrderByDescending(x => x.Supplier),
                ["ContractNotes"] = (query, ascending) => ascending ? query.OrderBy(x => x.ContractNotes) : query.OrderByDescending(x => x.ContractNotes),
                ["SortableDate"] = (query, ascending) => ascending ? query.OrderBy(x => x.SortableDate) : query.OrderByDescending(x => x.SortableDate)
            };
        }        
        private IQueryable<Models.Electric.ElectricDBModels.CE_ElectricContracts> BuildElectricQuery(long? supplierFilter, DateTime? fromDate, DateTime? toDate, string searchValue)
        {
            var query = db.CE_ElectricContracts.AsQueryable();

            // Apply supplier filter
            if (supplierFilter.HasValue)
            {
                query = query.Where(c => c.SupplierId == supplierFilter.Value);
            }

            // Apply date range filter at database level
            if (fromDate.HasValue && toDate.HasValue)
            {
                // Convert dates to strings for database comparison (dd/MM/yyyy format)
                string fromDateStr = fromDate.Value.ToString("dd/MM/yyyy");
                string toDateStr = toDate.Value.ToString("dd/MM/yyyy");
                
                // For dd/MM/yyyy format, we need to convert to yyyyMMdd for proper string comparison
                string fromDateComparable = fromDate.Value.ToString("yyyyMMdd");
                string toDateComparable = toDate.Value.ToString("yyyyMMdd");
                
                // Filter by converting InputDate to comparable format for proper date range filtering
                query = query.Where(e => e.InputDate != null && 
                    e.InputDate.Length == 10 && // Ensure it's in dd/MM/yyyy format
                    string.Compare(e.InputDate.Substring(6, 4) + e.InputDate.Substring(3, 2) + e.InputDate.Substring(0, 2), fromDateComparable) >= 0 &&
                    string.Compare(e.InputDate.Substring(6, 4) + e.InputDate.Substring(3, 2) + e.InputDate.Substring(0, 2), toDateComparable) <= 0);
            }

            // Apply search filter at database level for better performance
            if (!string.IsNullOrWhiteSpace(searchValue) && IsDatabaseSearchSupported(searchValue))
            {
                query = query.Where(e =>
                    e.BusinessName.Contains(searchValue) ||
                    e.CustomerName.Contains(searchValue) ||
                    e.PostCode.Contains(searchValue) ||
                    e.EmailAddress.Contains(searchValue) ||
                    e.ContractNotes.Contains(searchValue) ||
                    e.MPAN.Contains(searchValue) ||
                    // Add agent search by joining with lookup tables
                    (e.Department == "In House" && e.CloserId.HasValue &&
                     db.CE_Sector.Any(s => s.SectorID == e.CloserId && s.SectorType == "closer" && s.Name.Contains(searchValue))) ||
                    (e.Department == "Brokers" && e.BrokerageStaffId.HasValue &&
                     db.CE_BrokerageStaff.Any(bs => bs.BrokerageStaffID == e.BrokerageStaffId && bs.BrokerageStaffName.Contains(searchValue))) ||
                    (e.Department == "Introducers" && e.SubIntroducerId.HasValue &&
                     db.CE_SubIntroducer.Any(si => si.SubIntroducerID == e.SubIntroducerId && si.SubIntroducerName.Contains(searchValue))));
            }

            return query;
        }

        private IQueryable<Models.Gas.GasDBModels.CE_GasContracts> BuildGasQuery(long? supplierFilter, DateTime? fromDate, DateTime? toDate, string searchValue)
        {
            var query = db.CE_GasContracts.AsQueryable();

            // Apply supplier filter
            if (supplierFilter.HasValue)
            {
                query = query.Where(c => c.SupplierId == supplierFilter.Value);
            }

            // Apply date range filter at database level
            if (fromDate.HasValue && toDate.HasValue)
            {
                // Convert dates to strings for database comparison (dd/MM/yyyy format)
                string fromDateStr = fromDate.Value.ToString("dd/MM/yyyy");
                string toDateStr = toDate.Value.ToString("dd/MM/yyyy");
                
                // For dd/MM/yyyy format, we need to convert to yyyyMMdd for proper string comparison
                string fromDateComparable = fromDate.Value.ToString("yyyyMMdd");
                string toDateComparable = toDate.Value.ToString("yyyyMMdd");
                
                // Filter by converting InputDate to comparable format for proper date range filtering
                query = query.Where(g => g.InputDate != null && 
                    g.InputDate.Length == 10 && // Ensure it's in dd/MM/yyyy format
                    string.Compare(g.InputDate.Substring(6, 4) + g.InputDate.Substring(3, 2) + g.InputDate.Substring(0, 2), fromDateComparable) >= 0 &&
                    string.Compare(g.InputDate.Substring(6, 4) + g.InputDate.Substring(3, 2) + g.InputDate.Substring(0, 2), toDateComparable) <= 0);
            }

            // Apply search filter at database level for better performance
            if (!string.IsNullOrWhiteSpace(searchValue) && IsDatabaseSearchSupported(searchValue))
            {
                query = query.Where(g =>
                    g.BusinessName.Contains(searchValue) ||
                    g.CustomerName.Contains(searchValue) ||
                    g.PostCode.Contains(searchValue) ||
                    g.EmailAddress.Contains(searchValue) ||
                    g.ContractNotes.Contains(searchValue) ||
                    g.MPRN.Contains(searchValue) ||
                    // Add agent search by joining with lookup tables
                    (g.Department == "In House" && g.CloserId.HasValue &&
                     db.CE_Sector.Any(s => s.SectorID == g.CloserId && s.SectorType == "closer" && s.Name.Contains(searchValue))) ||
                    (g.Department == "Brokers" && g.BrokerageStaffId.HasValue &&
                     db.CE_BrokerageStaff.Any(bs => bs.BrokerageStaffID == g.BrokerageStaffId && bs.BrokerageStaffName.Contains(searchValue))) ||
                    (g.Department == "Introducers" && g.SubIntroducerId.HasValue &&
                     db.CE_SubIntroducer.Any(si => si.SubIntroducerID == g.SubIntroducerId && si.SubIntroducerName.Contains(searchValue))));
            }

            return query;
        }

        private bool IsDatabaseSearchSupported(string searchValue)
        {
            // Only apply database-level search for simple text searches
            // Complex searches with special characters should be done in memory
            return !string.IsNullOrWhiteSpace(searchValue) &&
                   !searchValue.Contains("%") && 
                   !searchValue.Contains("_") &&
                   !searchValue.Contains("*");
        }


        private async Task<List<PreSalesMasterDashboardRowViewModel>> ProcessCombinedContractsOptimized(List<Models.Electric.ElectricDBModels.CE_ElectricContracts> electricContracts, List<Models.Gas.GasDBModels.CE_GasContracts> gasContracts)
        {
            var result = new List<PreSalesMasterDashboardRowViewModel>();

            // Pre-load all required lookup data to avoid N+1 queries
            var supplierIds = electricContracts.Select(e => e.SupplierId)
                .Union(gasContracts.Select(g => g.SupplierId))
                .Distinct()
                .ToList();

            var suppliers = await db.CE_Supplier
                .Where(s => supplierIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name);

            // Pre-load agent data
            var allAgentIds = electricContracts.SelectMany(e => new[] { e.CloserId, e.BrokerageStaffId, e.SubIntroducerId })
                .Union(gasContracts.SelectMany(g => new[] { g.CloserId, g.BrokerageStaffId, g.SubIntroducerId }))
                .Where(id => id.HasValue)
                .Distinct()
                .ToList();

            var closers = await db.CE_Sector
                .Where(s => allAgentIds.Contains(s.SectorID) && s.SectorType == "closer")
                .ToDictionaryAsync(s => s.SectorID, s => s.Name);

            var brokerageStaff = await db.CE_BrokerageStaff
                .Where(bs => allAgentIds.Contains(bs.BrokerageStaffID))
                .ToDictionaryAsync(bs => bs.BrokerageStaffID, bs => bs.BrokerageStaffName);

            var introducers = await db.CE_SubIntroducer
                .Where(si => allAgentIds.Contains(si.SubIntroducerID))
                .ToDictionaryAsync(si => si.SubIntroducerID, si => si.SubIntroducerName);

            // Group by EId to handle dual contracts efficiently
            var allEIds = electricContracts.Select(e => e.EId).Union(gasContracts.Select(g => g.EId)).Distinct().ToList();

            foreach (var eId in allEIds)
            {
                var electricContract = electricContracts.FirstOrDefault(e => e.EId == eId);
                var gasContract = gasContracts.FirstOrDefault(g => g.EId == eId);

                bool isDual = electricContract != null && gasContract != null;

                if (isDual)
                {
                    // Create dual contract entry
                    var dualContract = CreateDualContractViewModelOptimized(electricContract, gasContract, suppliers, closers, brokerageStaff, introducers);
                    result.Add(dualContract);
                }
                else
                {
                    // Create single contract entry
                    if (electricContract != null)
                    {
                        var electricViewModel = CreateSingleContractViewModelOptimized(electricContract, "Electric", suppliers, closers, brokerageStaff, introducers);
                        result.Add(electricViewModel);
                    }
                    if (gasContract != null)
                    {
                        var gasViewModel = CreateSingleContractViewModelOptimized(gasContract, "Gas", suppliers, closers, brokerageStaff, introducers);
                        result.Add(gasViewModel);
                    }
                }
            }

            return result;
        }

        private PreSalesMasterDashboardRowViewModel CreateDualContractViewModelOptimized(
            Models.Electric.ElectricDBModels.CE_ElectricContracts electricContract, 
            Models.Gas.GasDBModels.CE_GasContracts gasContract,
            Dictionary<long, string> suppliers,
            Dictionary<int, string> closers,
            Dictionary<int, string> brokerageStaff,
            Dictionary<int, string> introducers)
        {
            // Get agent information (use electric contract data as primary)
            string agentName = GetAgentNameOptimized(electricContract.Department, electricContract.CloserId, electricContract.BrokerageStaffId, electricContract.SubIntroducerId, closers, brokerageStaff, introducers);

            // Get supplier names
            string electricSupplier = suppliers.TryGetValue(electricContract.SupplierId, out string electricSupplierName) ? electricSupplierName : "N/A";
            string gasSupplier = suppliers.TryGetValue(gasContract.SupplierId, out string gasSupplierName) ? gasSupplierName : "N/A";

            // Format dates for display
            string electricInputDate = ParserHelper.FormatDateForDisplay(electricContract.InputDate ?? "N/A");
            string gasInputDate = ParserHelper.FormatDateForDisplay(gasContract.InputDate ?? "N/A");

            // Parse dates for sorting (dd/MM/yyyy format)
            DateTime electricDate = ParserHelper.ParseDateForSorting(electricContract.InputDate ?? "N/A");
            DateTime gasDate = ParserHelper.ParseDateForSorting(gasContract.InputDate ?? "N/A");
            DateTime sortableDate = electricDate > gasDate ? electricDate : gasDate;

            return new PreSalesMasterDashboardRowViewModel
            {
                EId = electricContract.EId,
                Agent = agentName,
                BusinessName = electricContract.BusinessName ?? gasContract.BusinessName ?? "N/A",
                CustomerName = electricContract.CustomerName ?? gasContract.CustomerName ?? "N/A",
                PostCode = electricContract.PostCode ?? gasContract.PostCode ?? "N/A",
                InputDate = $"Electric: {electricInputDate}<br>Gas: {gasInputDate}",
                Duration = $"Electric: {electricContract.Duration ?? "N/A"}<br>Gas: {gasContract.Duration ?? "N/A"}",
                Uplift = $"Electric: {electricContract.Uplift ?? "N/A"}<br>Gas: {gasContract.Uplift ?? "N/A"}",
                InputEAC = $"Electric: {electricContract.InputEAC ?? "N/A"}<br>Gas: {gasContract.InputEAC ?? "N/A"}",
                Email = electricContract.EmailAddress ?? gasContract.EmailAddress ?? "N/A",
                Supplier = $"Electric: {electricSupplier}<br>Gas: {gasSupplier}",
                ContractNotes = electricContract.ContractNotes ?? gasContract.ContractNotes ?? "N/A",
                Type = "Dual",
                SortableDate = sortableDate,
                MPAN = electricContract.MPAN ?? "N/A",
                MPRN = gasContract.MPRN ?? "N/A"
            };
        }

        private PreSalesMasterDashboardRowViewModel CreateSingleContractViewModelOptimized(
            dynamic contract, 
            string contractType,
            Dictionary<long, string> suppliers,
            Dictionary<int, string> closers,
            Dictionary<int, string> brokerageStaff,
            Dictionary<int, string> introducers)
        {
            string agentName = GetAgentNameOptimized(contract.Department, contract.CloserId, contract.BrokerageStaffId, contract.SubIntroducerId, closers, brokerageStaff, introducers);
            string supplierName = suppliers.TryGetValue(contract.SupplierId, out string supplierNameValue) ? supplierNameValue : "N/A";
            string inputDate = ParserHelper.FormatDateForDisplay(contract.InputDate ?? "N/A");

            DateTime sortableDate = ParserHelper.ParseDateForSorting(contract.InputDate ?? "N/A");

            return new PreSalesMasterDashboardRowViewModel
            {
                EId = contract.EId,
                Agent = agentName,
                BusinessName = contract.BusinessName ?? "N/A",
                CustomerName = contract.CustomerName ?? "N/A",
                PostCode = contract.PostCode ?? "N/A",
                InputDate = inputDate,
                Duration = contract.Duration ?? "N/A",
                Uplift = contract.Uplift ?? "N/A",
                InputEAC = contract.InputEAC ?? "N/A",
                Email = contract.EmailAddress ?? "N/A",
                Supplier = supplierName,
                ContractNotes = contract.ContractNotes ?? "N/A",
                Type = contractType,
                SortableDate = sortableDate,
                MPAN = contractType == "Electric" ? (contract.MPAN ?? "N/A") : "N/A",
                MPRN = contractType == "Gas" ? (contract.MPRN ?? "N/A") : "N/A"
            };
        }

        private string GetAgentNameOptimized(string department, int? closerId, int? brokerageStaffId, int? subIntroducerId, 
            Dictionary<int, string> closers, Dictionary<int, string> brokerageStaff, Dictionary<int, string> introducers)
        {
            if (department == "In House" && closerId.HasValue)
            {
                return closers.TryGetValue(closerId.Value, out string closerName) ? closerName : "-";
            }
            else if (department == "Brokers" && brokerageStaffId.HasValue)
            {
                return brokerageStaff.TryGetValue(brokerageStaffId.Value, out string staffName) ? staffName : "-";
            }
            else if (department == "Introducers" && subIntroducerId.HasValue)
            {
                return introducers.TryGetValue(subIntroducerId.Value, out string introducerName) ? introducerName : "-";
            }

            return "-";
        }

        private async Task<ContractStatusCounts> GetContractStatusCounts()
        {
            try
            {
                // Get all unique EIds to avoid counting dual contracts twice
                var allEIds = await db.CE_ElectricContracts
                    .Select(e => e.EId)
                    .Union(db.CE_GasContracts.Select(g => g.EId))
                    .ToListAsync();

                // Get electric contracts with their statuses
                var electricStatuses = await db.CE_ElectricContracts
                    .Where(e => allEIds.Contains(e.EId))
                    .Select(e => new { e.EId, e.PreSalesStatus })
                    .ToListAsync();

                // Get gas contracts with their statuses
                var gasStatuses = await db.CE_GasContracts
                    .Where(g => allEIds.Contains(g.EId))
                    .Select(g => new { g.EId, g.PreSalesStatus })
                    .ToListAsync();

                // Combine statuses for each EId (prioritize electric status if both exist)
                var contractStatuses = new Dictionary<string, string>();
                
                foreach (var electric in electricStatuses)
                {
                    contractStatuses[electric.EId] = electric.PreSalesStatus ?? "";
                }
                
                foreach (var gas in gasStatuses)
                {
                    if (!contractStatuses.ContainsKey(gas.EId))
                    {
                        contractStatuses[gas.EId] = gas.PreSalesStatus ?? "";
                    }
                }

                // Count statuses using optimized string matching
                int submittedCount = 0;
                int rejectedCount = 0;
                int pendingCount = 0;

                foreach (var status in contractStatuses.Values)
                {
                    if (string.IsNullOrWhiteSpace(status))
                        continue;

                    string statusLower = status.ToLowerInvariant();

                    // Submitted: Contains "submitted" or "overturned"
                    if (statusLower.Contains("submitted") || statusLower.Contains("overturned"))
                    {
                        submittedCount++;
                    }
                    // Rejected: Contains "duplicate contract", "fail", "incorrect", "not supported", "rejected", or "failed"
                    else if (statusLower.Contains("duplicate contract") || 
                             statusLower.Contains("fail") || 
                             statusLower.Contains("incorrect") || 
                             statusLower.Contains("not supported") || 
                             statusLower.Contains("rejected") || 
                             statusLower.Contains("failed"))
                    {
                        rejectedCount++;
                    }
                    // Pending: Contains "contract to be checked", "ready to submit", or "awaiting"
                    else if (statusLower.Contains("contract to be checked") || 
                             statusLower.Contains("ready to submit") || 
                             statusLower.Contains("awaiting"))
                    {
                        pendingCount++;
                    }
                }

                return new ContractStatusCounts
                {
                    SubmittedCount = submittedCount,
                    RejectedCount = rejectedCount,
                    PendingCount = pendingCount
                };
            }
            catch (Exception ex)
            {
                Logger.Log("GetContractStatusCounts error: " + ex);
                return new ContractStatusCounts
                {
                    SubmittedCount = 0,
                    RejectedCount = 0,
                    PendingCount = 0
                };
            }
        }

        private class ContractStatusCounts
        {
            public int SubmittedCount { get; set; }
            public int RejectedCount { get; set; }
            public int PendingCount { get; set; }
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> GetFilteredContractStatusCounts()
        {
            try
            {
                string inputDateFrom = Request.Form["inputDateFrom"];
                string inputDateTo = Request.Form["inputDateTo"];
                string supplierId = Request.Form["supplierId"];

                DateTime? fromDate = null;
                DateTime? toDate = null;
                
                if (!string.IsNullOrWhiteSpace(inputDateFrom) && DateTime.TryParse(inputDateFrom, out DateTime from))
                    fromDate = from.Date;
                
                if (!string.IsNullOrWhiteSpace(inputDateTo) && DateTime.TryParse(inputDateTo, out DateTime to))
                    toDate = to.Date.AddDays(1).AddTicks(-1);

                long? supplierFilter = null;
                if (!string.IsNullOrWhiteSpace(supplierId) && long.TryParse(supplierId, out long supplierIdLong))
                    supplierFilter = supplierIdLong;

                var statusCounts = await GetContractStatusCountsWithFilters(supplierFilter, fromDate, toDate);

                return Json(new
                {
                    success = true,
                    submittedCount = statusCounts.SubmittedCount,
                    rejectedCount = statusCounts.RejectedCount,
                    pendingCount = statusCounts.PendingCount
                });
            }
            catch (Exception ex)
            {
                Logger.Log("GetFilteredContractStatusCounts error: " + ex);
                return Json(new
                {
                    success = false,
                    message = "Error fetching filtered contract status counts."
                });
            }
        }

        private async Task<ContractStatusCounts> GetContractStatusCountsWithFilters(long? supplierFilter, DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                // Build filtered queries for both electric and gas contracts
                var electricQuery = BuildElectricQuery(supplierFilter, fromDate, toDate, null);
                var gasQuery = BuildGasQuery(supplierFilter, fromDate, toDate, null);

                // Get all unique EIds from filtered results to avoid counting dual contracts twice
                var electricEIds = await electricQuery.Select(e => e.EId).ToListAsync();
                var gasEIds = await gasQuery.Select(g => g.EId).ToListAsync();
                var allEIds = electricEIds.Union(gasEIds).ToList();

                if (!allEIds.Any())
                {
                    return new ContractStatusCounts
                    {
                        SubmittedCount = 0,
                        RejectedCount = 0,
                        PendingCount = 0
                    };
                }

                // Get electric contracts with their statuses (filtered)
                var electricStatuses = await electricQuery
                    .Where(e => allEIds.Contains(e.EId))
                    .Select(e => new { e.EId, e.PreSalesStatus })
                    .ToListAsync();

                // Get gas contracts with their statuses (filtered)
                var gasStatuses = await gasQuery
                    .Where(g => allEIds.Contains(g.EId))
                    .Select(g => new { g.EId, g.PreSalesStatus })
                    .ToListAsync();

                // Combine statuses for each EId (prioritize electric status if both exist)
                var contractStatuses = new Dictionary<string, string>();
                
                foreach (var electric in electricStatuses)
                {
                    contractStatuses[electric.EId] = electric.PreSalesStatus ?? "";
                }
                
                foreach (var gas in gasStatuses)
                {
                    if (!contractStatuses.ContainsKey(gas.EId))
                    {
                        contractStatuses[gas.EId] = gas.PreSalesStatus ?? "";
                    }
                }

                // Count statuses using optimized string matching
                int submittedCount = 0;
                int rejectedCount = 0;
                int pendingCount = 0;

                foreach (var status in contractStatuses.Values)
                {
                    if (string.IsNullOrWhiteSpace(status))
                        continue;

                    string statusLower = status.ToLowerInvariant();

                    // Submitted: Contains "submitted" or "overturned"
                    if (statusLower.Contains("submitted") || statusLower.Contains("overturned"))
                    {
                        submittedCount++;
                    }
                    // Rejected: Contains "duplicate contract", "fail", "incorrect", "not supported", "rejected", or "failed"
                    else if (statusLower.Contains("duplicate contract") || 
                             statusLower.Contains("fail") || 
                             statusLower.Contains("incorrect") || 
                             statusLower.Contains("not supported") || 
                             statusLower.Contains("rejected") || 
                             statusLower.Contains("failed"))
                    {
                        rejectedCount++;
                    }
                    // Pending: Contains "contract to be checked", "ready to submit", or "awaiting"
                    else if (statusLower.Contains("contract to be checked") || 
                             statusLower.Contains("ready to submit") || 
                             statusLower.Contains("awaiting"))
                    {
                        pendingCount++;
                    }
                }

                return new ContractStatusCounts
                {
                    SubmittedCount = submittedCount,
                    RejectedCount = rejectedCount,
                    PendingCount = pendingCount
                };
            }
            catch (Exception ex)
            {
                Logger.Log("GetContractStatusCountsWithFilters error: " + ex);
                return new ContractStatusCounts
                {
                    SubmittedCount = 0,
                    RejectedCount = 0,
                    PendingCount = 0
                };
            }
        }


        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> GetContractNotesOnly(string eId, string contractType)
        {
            try
            {
                var model = new EditContractNotesOnlyViewModel
                {
                    EId = eId,
                    ContractType = contractType,
                    ContractNotes = "",
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

                        // Check if it is also a dual contract
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

                        // Check if it is also a dual contract
                        var electricalContract = await db.CE_ElectricContracts.Where(c => c.EId == eId).FirstOrDefaultAsync();
                        model.IsDualContract = electricalContract != null;
                    }
                }
                else if (contractType == "Dual")
                {
                    // For dual contracts, get data from both electric and gas
                    var electricContract = await db.CE_ElectricContracts
                        .Where(c => c.EId == eId)
                        .FirstOrDefaultAsync();
                    
                    var gasContract = await db.CE_GasContracts
                        .Where(c => c.EId == eId)
                        .FirstOrDefaultAsync();

                    if (electricContract != null)
                    {
                        model.ContractNotes = electricContract.ContractNotes ?? "";
                        model.IsDualContract = true;
                    }
                }

                return JsonResponse.Ok(model);
            }
            catch (Exception ex)
            {
                Logger.Log("GetContractNotesOnly error: " + ex);
                return JsonResponse.Fail("Could not fetch contract notes.");
            }
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> UpdateContractNotesOnly(EditContractNotesOnlyViewModel model)
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
                        await db.SaveChangesAsync();
                    }
                }
                else if (model.ContractType == "Dual")
                {
                    // Update both electric and gas contracts for dual
                    var electricContract = await db.CE_ElectricContracts
                        .Where(c => c.EId == model.EId)
                        .FirstOrDefaultAsync();
                    
                    var gasContract = await db.CE_GasContracts
                        .Where(c => c.EId == model.EId)
                        .FirstOrDefaultAsync();

                    if (electricContract != null)
                    {
                        electricContract.ContractNotes = model.ContractNotes;
                    }

                    if (gasContract != null)
                    {
                        gasContract.ContractNotes = model.ContractNotes;
                    }

                    if (electricContract != null || gasContract != null)
                    {
                        await db.SaveChangesAsync();
                    }
                }

                return JsonResponse.Ok("Contract notes updated successfully.");
            }
            catch (Exception ex)
            {
                Logger.Log("UpdateContractNotes error: " + ex);
                return JsonResponse.Fail("Could not update contract notes.");
            }
        }

        /// <summary>
        /// Get column names in the exact order they appear in the DataTable
        /// </summary>
        private string[] GetColumnNames()
        {
            return new[] { 
                "Edit", "Agent", "BusinessName", "CustomerName", "PostCode", 
                "InputDate", "Duration", "Uplift", "InputEAC", "Email", 
                "Supplier", "ContractNotes", "SortableDate" 
            };
        }


    }
}


