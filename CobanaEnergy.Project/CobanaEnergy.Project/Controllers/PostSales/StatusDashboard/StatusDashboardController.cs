using CobanaEnergy.Project.Common;
using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Filters;
using CobanaEnergy.Project.Helpers;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.Accounts;
using CobanaEnergy.Project.Models.Accounts.InvoiceSupplierDashboard;
using CobanaEnergy.Project.Models.Accounts.MasterDashboard.AccountMasterDashboard;
using CobanaEnergy.Project.Models.Accounts.SuppliersModels.BGB.DBModel;
using CobanaEnergy.Project.Models.AccountsDBModel;
using CobanaEnergy.Project.Models.Electric.ElectricDBModels.snapshot;
using CobanaEnergy.Project.Models.EmailTemplateLookup;
using CobanaEnergy.Project.Models.Gas.EditGas;
using CobanaEnergy.Project.Models.Gas.GasDBModels;
using CobanaEnergy.Project.Models.PostSales.Entities;
using CobanaEnergy.Project.Models.PostSales.PostSalesContract;
using CobanaEnergy.Project.Models.PostSales.StatusDashboard;
using CobanaEnergy.Project.Models.Supplier.SupplierDBModels;
using CobanaEnergy.Project.Models.Supplier.SupplierDBModels.snapshot;
using CobanaEnergy.Project.Models.Supplier.SupplierDBModels.snapshot_Gas;
using CobanaEnergy.Project.Models.Supplier.SupplierSnapshots_Gas;
using CobanaEnergy.Project.Service.HelperUtilityService;
using Logic;
using Logic.LockManager;
using Logic.ResponseModel.Helper;
using Microsoft.AspNet.Identity;
using Org.BouncyCastle.Bcpg.OpenPgp;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Odbc;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Controllers.PostSales.StatusDashboard
{
    [Authorize(Roles = "Post-sales")]
    public class StatusDashboardController : BaseController
    {
        private readonly ApplicationDBContext _db;
        public StatusDashboardController(ApplicationDBContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult> Index()
        {
            var suppliers = await _db.CE_Supplier
               .Select(s => new SelectListItem
               {
                   Value = s.Id.ToString(),
                   Text = s.Name
               })
               .ToListAsync();

            var model = new StatusDashboardViewModel
            {
                Suppliers = suppliers
            };

            return View("~/Views/PostSales/StatusDashboard/StatusDashboard.cshtml", model);
        }


        private async Task EnsurePreSalesMovedToPostSalesAsync()
        {
            // PreSale column me "submitted" or "overturned" check karna hai
            var submittedKeys = new[] { "submitted", "overturned" };

            // Electric contracts
            var electricPresales = await _db.CE_ElectricContracts
                .Where(ec => !string.IsNullOrEmpty(ec.PreSalesStatus) &&
                             submittedKeys.Contains(ec.PreSalesStatus.ToLower()))
                .Select(ec => new { ec.EId, Type = "Electric" })
                .ToListAsync();

            // Contracts
            var gasPresales = await _db.CE_GasContracts
                .Where(gc => !string.IsNullOrEmpty(gc.PreSalesStatus) &&
                             submittedKeys.Contains(gc.PreSalesStatus.ToLower()))
                .Select(gc => new { gc.EId, Type = "Gas" })
                .ToListAsync();

            // Combine both
            var allPresales = electricPresales.Concat(gasPresales).ToList();

            foreach (var s in allPresales)
            {
                var eId = s.EId;
                var type = s.Type;

                // Check if already present in post-sales statuses
                var existingStatus = await _db.CE_ContractStatuses
                    .Where(cs => cs.EId == eId && cs.Type == type)
                    .OrderByDescending(cs => cs.ModifyDate)
                    .FirstOrDefaultAsync();

                if (existingStatus == null)
                {
                    var newStatus = new CE_ContractStatuses
                    {
                        EId = eId,
                        Type = s.Type,
                        ContractStatus = "Pending",
                        PaymentStatus = existingStatus?.PaymentStatus,
                        ModifyDate = DateTime.Now,
                        PostSalesCreationDate = DateTime.Now
                    };

                    _db.CE_ContractStatuses.Add(newStatus);
                }
            }

            await _db.SaveChangesAsync();
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> GetPostSalesContracts(StatusQueryParams q)
        {
            await EnsurePreSalesMovedToPostSalesAsync();

            // -------------------------------------------
            // 1. Combine Electric + Contracts
            // -------------------------------------------
            var combined = GetCombinedContracts();

            // -------------------------------------------
            // 2. Apply Filters
            // -------------------------------------------
            combined = ApplyFilters(combined, q);

            // Materialize after filters
            var list = await combined.ToListAsync();

            // Date filters
            if (q.DateFrom.HasValue)
                list = list.Where(x => DateTime.TryParse(x.InputDate, out var d) && d >= q.DateFrom.Value).ToList();

            if (q.DateTo.HasValue)
                list = list.Where(x => DateTime.TryParse(x.InputDate, out var d) && d <= q.DateTo.Value).ToList();

            // -------------------------------------------
            // 3. Load Related Data
            // -------------------------------------------
            var eIds = list.Select(x => x.EId).Distinct().ToList();

            var crs = await _db.CE_CommissionAndReconciliation.Where(c => eIds.Contains(c.EId)).ToListAsync();
            var statuses = await _db.CE_ContractStatuses
                .Where(s => eIds.Contains(s.EId))
                .GroupBy(s => new { s.EId, s.Type })
                .Select(g => g.OrderByDescending(x => x.ModifyDate).FirstOrDefault())
                .ToListAsync();

            var postSaleObj = await _db.CE_PostSaleObjections
                .Where(s => eIds.Contains(s.EId))
                .GroupBy(s => new { s.EId, s.ContractType })
                .Select(g => g.OrderByDescending(x => x.ModifyDate).FirstOrDefault())
                .ToListAsync();

            var supplierContacts = await _db.CE_SupplierContacts.ToListAsync();

            // exclude dead/ended
            var activeEids = statuses
                .Where(s => s != null && !new[] { "ended", "dead" }.Contains((s.ContractStatus ?? "").ToLower()))
                .Select(s => s.EId)
                .ToHashSet();

            var contracts = list.Where(c => activeEids.Contains(c.EId)).ToList();

            // ContractStatus filter
            if (!string.IsNullOrEmpty(q.ContractStatus))
            {
                var target = q.ContractStatus.ToLower();
                var matched = statuses
                    .Where(s => s != null && (s.ContractStatus ?? "").ToLower() == target)
                    .Select(s => s.EId).ToHashSet();

                contracts = contracts.Where(c => matched.Contains(c.EId)).ToList();
            }

            // -------------------------------------------
            // 4. Searching
            // -------------------------------------------
            contracts = ApplySearching(contracts, q.Search?.Value);

            // -------------------------------------------
            // 5. Sorting
            // -------------------------------------------
            contracts = ApplySorting(contracts, q, crs, statuses, postSaleObj);

            // -------------------------------------------
            // 6. Counter
            // -------------------------------------------

            var statusSummary = GetContractStatusSummary(contracts, statuses);


            // -------------------------------------------
            // 7. Paging
            // -------------------------------------------
            var total = contracts.Count;
            var page = contracts.Skip(q.Start).Take(q.Length).ToList();

            // -------------------------------------------
            // 7. Projection
            // -------------------------------------------
            var rows = ProjectRows(page, statuses, crs, postSaleObj, supplierContacts);

            return Json(new
            {
                draw = q.Draw,
                recordsTotal = total,
                recordsFiltered = total,
                data = rows,
                contractCount = total,
                statusSummary = statusSummary
            });
        }


        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> UpdatePostSalesRow(UpdatePostSalesFieldDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.EId))
                return Json(new { success = false, message = "Invalid payload" });

            bool updated = false;

            try
            {
                // 1. Update Contract Email
                updated |= await UpdateContractEmail(dto);

                // 2. Reconciliation (insert or update)
                var cr = await UpsertReconciliation(dto);
                updated |= cr != null;

                // 3. Metrics (always ensure entry)
                updated |= await UpsertMetrics(cr);

                // 4. Contract Status
                updated |= await UpdateContractStatus(dto);

                // 5. Objection (insert or update + increment)
                updated |= await UpsertObjection(dto);

                // 6. Always insert StatusDashboardLogs (new log entry)
                updated |= InsertStatusDashboardLog(dto, User.Identity?.Name ?? "System");

                if (updated)
                {
                    await _db.SaveChangesAsync();
                    return JsonResponse.Ok(message: "Contract updated successfully.");
                }

                return JsonResponse.Ok(message: "No update applied");
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet]
        public JsonResult GetQueryTypes(string supplier)
        {
            if (string.IsNullOrEmpty(supplier))
                return Json(new List<string>(), JsonRequestBehavior.AllowGet);

            if (HelperService.SupplierQueryTypes.TryGetValue(supplier, out var queries))
                return Json(queries, JsonRequestBehavior.AllowGet);

            return Json(new List<string>(), JsonRequestBehavior.AllowGet);
        }

        #region [Edit Contract]

        [HttpGet]
        public async Task<ActionResult> EditContractPostSales(string id, string type)
        {
            if (string.IsNullOrWhiteSpace(id))
                return HttpNotFound();
            try
            {

                if (string.IsNullOrWhiteSpace(id)) return HttpNotFound();

                try
                {
                    var postSalesObj = await _db.CE_PostSaleObjections
                        .FirstOrDefaultAsync(x => x.EId == id && x.ContractType == type);
                    var contractStatus = await _db.CE_ContractStatuses
                        .FirstOrDefaultAsync(x => x.EId == id && x.Type == type);
                    var account = await _db.CE_Accounts
                        .FirstOrDefaultAsync(a => a.EId == id && a.Type == type);
                    var crs = await _db.CE_CommissionAndReconciliation
                        .FirstOrDefaultAsync(c => c.EId == id && c.contractType == type);

                    var model = new PostSalesEditViewModel();

                    if (string.Equals(type, "Electric", StringComparison.OrdinalIgnoreCase))
                    {
                        // ELECTRIC
                        var contract = await GetContractDetailsAsync(id, type, account);
                        if (contract == null) return HttpNotFound();

                        var snapshot = await _db.CE_ElectricSupplierSnapshots
                            .Include(s => s.CE_ElectricSupplierProductSnapshots)
                            .Include(s => s.CE_ElectricSupplierContactSnapshots)
                            .Include(s => s.CE_ElectricSupplierUpliftSnapshots)
                            .FirstOrDefaultAsync(s => s.EId == contract.EId);
                        if (snapshot == null) return HttpNotFound();

                        var savedProductSnapshot = snapshot.CE_ElectricSupplierProductSnapshots
                            .FirstOrDefault(p => p.ProductId == contract.ProductId);

                        #region [Campain]

                        DateTime? contractInputDate = null;

                        if (DateTime.TryParse(contract.InputDate, out var parsedInputDate))
                            contractInputDate = parsedInputDate;
                        string campaignName = null;
                        if (contractInputDate.HasValue)
                        {
                            var campaign = await _db.CE_Campaigns
                                .FirstOrDefaultAsync(x =>
                                    x.SupplierId == contract.SupplierId &&
                                    x.ProductId == contract.ProductId &&
                                    x.StartDate <= contractInputDate.Value &&
                                    x.EndDate >= contractInputDate.Value);

                            if (campaign != null)
                            {
                                campaignName = campaign.CampaignName;
                            }
                        }

                        contract.CampaignName = campaignName ?? "N/A";

                        #endregion


                        // Map common
                        model = MapCommon(contract, account, contractStatus, postSalesObj, crs);

                        // Set Electric specifics (MPAN, patterns etc.)
                        model.ContractType = FuelType.Electric;
                        model.SupplyNumberLabel = "MPAN";
                        // Simple MPAN sanity check: 13 or 21 digits (you can tighten to official structure if needed)
                        model.SupplyNumberPattern = @"^\d{13}(\d{8})?$";
                        model.SupplyNumberTitle = "Please enter valid MPAN";
                        model.SupplyNumber = contract.MPXN;

                        // Supplier/Product
                        model.SupplierId = snapshot.SupplierId;
                        model.ProductId = savedProductSnapshot?.Id ?? 0;
                        model.SupplierCommsType = savedProductSnapshot?.SupplierCommsType ?? contract.SupplierCommsType;

                        // Snapshot map
                        model.SupplierSnapshot = MapSnapshot(snapshot);
                    }
                    else
                    {
                        // GAS
                        var contract = await GetContractDetailsAsync(id, type, account);
                        if (contract == null) return HttpNotFound();

                        var snapshot = await _db.CE_GasSupplierSnapshots
                            .Include(s => s.CE_GasSupplierProductSnapshots)
                            .Include(s => s.CE_GasSupplierContactSnapshots)
                            .Include(s => s.CE_GasSupplierUpliftSnapshots)
                            .FirstOrDefaultAsync(s => s.EId == contract.EId);
                        if (snapshot == null) return HttpNotFound();


                        #region [Campain]

                        DateTime? contractInputDate = null;

                        if (DateTime.TryParse(contract.InputDate, out var parsedInputDate))
                            contractInputDate = parsedInputDate;
                        string campaignName = null;
                        if (contractInputDate.HasValue)
                        {
                            var campaign = await _db.CE_Campaigns
                                .FirstOrDefaultAsync(x =>
                                    x.SupplierId == contract.SupplierId &&
                                    x.ProductId == contract.ProductId &&
                                    x.StartDate <= contractInputDate.Value &&
                                    x.EndDate >= contractInputDate.Value);

                            if (campaign != null)
                            {
                                campaignName = campaign.CampaignName;
                            }
                        }

                        contract.CampaignName = campaignName ?? "N/A";

                        #endregion


                        var savedProductSnapshot = snapshot.CE_GasSupplierProductSnapshots
                            .FirstOrDefault(p => p.ProductId == contract.ProductId);

                        model = MapCommon(contract, account, contractStatus, postSalesObj, crs);

                        model.ContractType = FuelType.Gas;
                        model.SupplyNumberLabel = "MPRN";
                        model.SupplyNumberPattern = @"^\d{6,10}$";
                        model.SupplyNumberTitle = "Please enter 6–10 digit MPRN or N/A";
                        model.SupplyNumber = contract.MPXN;

                        model.SupplierId = snapshot.SupplierId;
                        model.ProductId = savedProductSnapshot?.Id ?? 0;
                        model.SupplierCommsType = savedProductSnapshot?.SupplierCommsType ?? contract.SupplierCommsType;

                        model.SupplierSnapshot = MapSnapshot(snapshot);
                    }

                    return View("~/Views/PostSales/PostSalesContract/EditContractPostSales.cshtml", model);
                }
                catch (Exception ex)
                {
                    Logger.Log("Failed to Load Contract : " + ex.Message);
                    return JsonResponse.Fail("Failed to Load Contract");
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Failed to Load Gas Contract : " + ex.Message);
                return JsonResponse.Fail("Failed to Load Gas Contract");
            }
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> UpdatePostSalesContract(PostSalesEditViewModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.EId))
                return JsonResponse.Fail("Invalid payload");

            var userName = User.Identity?.Name ?? "System";

            try
            {
                // ---------- 1️⃣ Update Core Contract ----------
                if (model.ContractType == FuelType.Electric)
                {
                    var contract = await _db.CE_ElectricContracts.FirstOrDefaultAsync(x => x.EId == model.EId);
                    if (contract != null)
                    {
                        // Only editable fields per requirements
                        contract.Source = model.Source;
                        contract.BusinessName = model.BusinessName;
                        contract.CustomerName = model.CustomerName;
                        contract.PostCode = model.PostCode;
                        contract.PhoneNumber1 = model.PhoneNumber1;
                        contract.EmailAddress = model.EmailAddress;
                        contract.SalesType = model.SalesType;
                        contract.SalesTypeStatus = model.SalesTypeStatus;
                        contract.Uplift = model.Uplift;
                        contract.InputEAC = model.InputEAC;
                        contract.ContractNotes = model.ContractNotes;

                    }
                }
                else
                {
                    var contract = await _db.CE_GasContracts.FirstOrDefaultAsync(x => x.EId == model.EId);
                    if (contract != null)
                    {
                        contract.Source = model.Source;
                        contract.BusinessName = model.BusinessName;
                        contract.CustomerName = model.CustomerName;
                        contract.PostCode = model.PostCode;
                        contract.PhoneNumber1 = model.PhoneNumber1;
                        contract.EmailAddress = model.EmailAddress;
                        contract.SalesType = model.SalesType;
                        contract.SalesTypeStatus = model.SalesTypeStatus;
                        contract.Uplift = model.Uplift;
                        contract.InputEAC = model.InputEAC;
                        contract.ContractNotes = model.ContractNotes;

                    }
                }

                // ---------- 2️⃣ Reconciliation ----------
                var rec = await _db.CE_CommissionAndReconciliation
                    .FirstOrDefaultAsync(x => x.EId == model.EId && x.contractType == model.ContractType.ToString());

                if (rec == null)
                {
                    rec = new CE_CommissionAndReconciliation
                    {
                        EId = model.EId,
                        contractType = model.ContractType.ToString(),
                        StartDate = model.InitialStartDate,
                        CED = model.CED,
                        CED_COT = model.CEDCOT
                    };
                    _db.CE_CommissionAndReconciliation.Add(rec);
                }
                else
                {
                    rec.StartDate = model.InitialStartDate;
                    rec.CED = model.CED;
                    rec.CED_COT = model.CEDCOT;
                }

                // ---------- 3️⃣ Metrics ----------
                var metrics = await _db.CE_CommissionMetrics
                    .FirstOrDefaultAsync(m => m.ReconciliationId == rec.Id && m.contractType == rec.contractType);
                if (metrics == null)
                {
                    _db.CE_CommissionMetrics.Add(new CE_CommissionMetrics
                    {
                        ReconciliationId = rec.Id,
                        contractType = rec.contractType
                    });
                }

                // ---------- 4️⃣ Contract Status ----------
                var status = await _db.CE_ContractStatuses
                    .FirstOrDefaultAsync(s => s.EId == model.EId && s.Type == model.ContractType.ToString());
                if (status != null)
                {
                    status.ContractStatus = model.ContractStatus;
                    status.ModifyDate = DateTime.Now;
                }

                // ---------- 5️⃣ Objection ----------
                var objection = await _db.CE_PostSaleObjections
                    .FirstOrDefaultAsync(o => o.EId == model.EId && o.ContractType == model.ContractType.ToString());
                if (objection == null)
                {
                    _db.CE_PostSaleObjections.Add(new CE_PostSaleObjection
                    {
                        EId = model.EId,
                        ContractType = model.ContractType.ToString(),
                        QueryType = model.QueryType,
                        ObjectionDate = model.ObjectionDate,
                        ReAppliedDate = model.ReappliedDate,
                        FollowUpDate = model.FollowUpDate,
                        ProspectedSaleDate = model.ProspectedSaleDate,
                        ProspectedSaleNotes = model.ProspectedSaleNotes,
                        CreatedBy = userName,
                        CreatedDate = DateTime.Now,
                        ModifyDate = DateTime.Now,
                        ContractId = model.ContractId
                    });
                }
                else
                {
                    if (model.ObjectionDate != null)
                        objection.ObjectionCount += 1;
                    if (model.ReappliedDate != null)
                        objection.ReAppliedCount += 1;

                    objection.QueryType = model.QueryType;
                    objection.ObjectionDate = model.ObjectionDate;
                    objection.ReAppliedDate = model.ReappliedDate;
                    objection.FollowUpDate = model.FollowUpDate;
                    objection.ProspectedSaleDate = model.ProspectedSaleDate;
                    objection.ProspectedSaleNotes = model.ProspectedSaleNotes;
                    objection.ModifyBy = userName;
                    objection.ModifyDate = DateTime.Now;
                    objection.ContractId = model.ContractId;
                }

                // ---------- 6️⃣ Logs ----------
                var log = new CE_PostSalesLogs
                {
                    EId = model.EId,
                    ContractType = model.ContractType.ToString(),
                    ContractStatus = model.ContractStatus,
                    QueryType = model.QueryType,
                    CSD = model.InitialStartDate,
                    CED = model.CED,
                    COT = model.CEDCOT,
                    ReAppliedDate = model.ReappliedDate,
                    ObjectionDate = model.ObjectionDate,
                    CreatedBy = userName,
                    CreatedDate = DateTime.Now,
                    ContractNotes = model.ContractNotes
                };
                _db.CE_PostSalesLogs.Add(log);

                // ---------- 7️⃣ Save ----------
                await _db.SaveChangesAsync();
                return JsonResponse.Ok("Post Sales contract updated successfully.");

            }
            catch (Exception ex)
            {
                return JsonResponse.Fail($"Error: {ex.Message}");
            }
        }


        [HttpGet]
        public async Task<JsonResult> GetLogsPostSalesContract(string id, string type)
        {
            try
            {
                var logs = await _db.CE_PostSalesLogs
                    .Where(l => l.EId == id && l.ContractType == type)
                    .Select(l => new
                    {
                        l.CreatedBy,
                        l.CreatedDate,
                        l.ContractStatus,
                        l.CSD,
                        l.CED,
                        l.COT,
                        l.ObjectionDate,
                        l.ReAppliedDate,
                        l.ContractNotes,
                        l.QueryType
                    })
                    .OrderByDescending(l => l.CreatedDate)
                    .ToListAsync();

                var formatted = logs.Select(l => new
                {
                    Username = l.CreatedBy,
                    ActionDate = l.CreatedDate.ToString("dd/MM/yyyy HH:mm:ss"),
                    ContractStatus = l.ContractStatus ?? "-",
                    ContractNotes = l.ContractNotes ?? "-",
                    QueryType = l.QueryType ?? "-",
                    CSD = l.CSD ?? "-",
                    CED = l.CED ?? "-",
                    COT = l.COT ?? "-",
                    ObjectionDate = l.ObjectionDate ?? "-",
                    ReAppliedDate = l.ReAppliedDate ?? "-"
                });

                return JsonResponse.Ok(formatted);
            }
            catch (Exception ex)
            {
                Logger.Log("GetLogsForPostSales failed: " + ex.Message);
                return JsonResponse.Fail("Unable to retrieve post-sales logs.");
            }
        }

        #endregion

        #region [Helper Method]

        private PostSalesEditViewModel MapCommon(ContractViewModel contract, CE_Accounts account,
                    CE_ContractStatuses status, CE_PostSaleObjection ps, CE_CommissionAndReconciliation cr)
        {

            var effectiveCed = HelperUtility.GetEffectiveCED(contract, cr);

            string queryType = ps?.QueryType;
            var emailDetails = _db.CE_EmailTemplateLookups
                .FirstOrDefault(e => e.SupplierId == contract.SupplierId && e.QueryType == queryType);

            var emailList = _db.CE_SupplierContacts
                         .Where(sc => sc.SupplierId == contract.SupplierId
                                   && sc.Email != null
                                   && sc.Email != ""
                                   && sc.Email != " ")
                         .Select(sc => sc.Email.Trim())
                         .Distinct()
                         .ToList();

            var (agentName, agentEmail) = ResolveAgent(contract);

            var collaboration = ResolveCollaboration(contract);


            var vm = new PostSalesEditViewModel
            {
                EId = contract.EId,
                Id = contract.Id,
                AgentName = agentName,
                AgentEmail = agentEmail,
                CustomerName = contract.CustomerName,
                BusinessName = contract.BusinessName,
                BusinessDoorNumber = contract.BusinessDoorNumber,
                BusinessHouseName = contract.BusinessHouseName,
                Link = contract.Link,
                BusinessStreet = contract.BusinessStreet,
                BusinessTown = contract.BusinessTown,
                BusinessCounty = contract.BusinessCounty,
                PostCode = contract.PostCode,
                PhoneNumber1 = contract.PhoneNumber1,
                PhoneNumber2 = contract.PhoneNumber2,
                EmailAddress = contract.EmailAddress,
                CampaignName = contract.CampaignName,

                CED = effectiveCed.ToString("yyyy-MM-dd"),
                CEDCOT = cr?.CED_COT,
                InitialStartDate = cr?.StartDate ?? contract.StartDate,

                InputDate = ParserHelper.FormatDateForDisplay(contract.InputDate),
                Duration = contract.Duration,
                Uplift = contract.Uplift,
                InputEAC = contract.InputEAC,
                UnitRate = contract.UnitRate,
                OtherRate = contract.OtherRate,
                StandingCharge = contract.StandingCharge,
                ContractId = ps?.ContractId,
                SortCode = account?.SortCode ?? "",
                AccountNumber = account?.AccountNumber ?? "",
                CurrentSupplier = contract.CurrentSupplier,

                EMProcessor = contract.EMProcessor,
                ContractChecked = contract.ContractChecked,
                ContractAudited = contract.ContractAudited,
                Terminated = contract.Terminated,
                ContractNotes = contract.ContractNotes,
                Department = contract.Department,
                Source = contract.Source,
                SalesType = contract.SalesType,
                SalesTypeStatus = contract.SalesTypeStatus,
                PreSalesStatus = contract.PreSalesStatus,
                PreSalesFollowUpDate = contract.PreSalesFollowUpDate,

                // Brokerage
                BrokerageId = contract.BrokerageId,
                OfgemId = contract.OfgemId,

                // Dynamic
                CloserId = contract.CloserId,
                ReferralPartnerId = contract.ReferralPartnerId,
                SubReferralPartnerId = contract.SubReferralPartnerId,
                BrokerageStaffId = contract.BrokerageStaffId,
                IntroducerId = contract.IntroducerId,
                SubIntroducerId = contract.SubIntroducerId,
                SubBrokerageId = contract.SubBrokerageId,
                Collaboration = collaboration,
                LeadGeneratorId = contract.LeadGeneratorId,

                // Post sales
                ContractStatus = status?.ContractStatus,
                QueryType = ps?.QueryType,
                ObjectionCount = ps?.ObjectionCount,
                ObjectionDate = ps?.ObjectionDate,
                ReappliedCount = ps?.ReAppliedCount,
                ReappliedDate = ps?.ReAppliedDate,
                ProspectedSaleDate = ps?.ProspectedSaleDate,
                ProspectedSaleNotes = ps?.ProspectedSaleNotes,
                FollowUpDate = ps?.FollowUpDate,
                EmailList = emailList,
                EmailSubject = emailDetails?.Subject,
                EmailBody = emailDetails?.EmailBody,
                TopLine = contract.TopLine
            };

            return vm;
        }

        private SupplierSnapshotViewModel MapSnapshot(CE_GasSupplierSnapshots snapshot)
        {
            return new SupplierSnapshotViewModel
            {
                Id = snapshot.Id,
                SupplierId = snapshot.SupplierId,
                EId = snapshot.EId,
                SupplierName = snapshot.SupplierName,
                Link = snapshot.SupplierLink,
                Products = (snapshot.CE_GasSupplierProductSnapshots ?? Enumerable.Empty<CE_GasSupplierProductSnapshots>())
                    .Select(p => new SupplierProductSnapshotViewModel
                    {
                        Id = p.Id,
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        SupplierCommsType = p.SupplierCommsType,
                        StartDate = p.StartDate.ToString("dd/MM/yyyy"),
                        EndDate = p.EndDate.ToString("dd/MM/yyyy"),
                        Commission = p.Commission
                    }).ToList(),
                Uplifts = (snapshot.CE_GasSupplierUpliftSnapshots ?? Enumerable.Empty<CE_GasSupplierUpliftSnapshots>())
                    .Select(u => new SupplierUpliftSnapshotViewModel
                    {
                        Id = u.Id,
                        Uplift = u.Uplift,
                        FuelType = u.FuelType,
                        StartDate = u.StartDate,
                        EndDate = u.EndDate
                    }).ToList(),
                Contacts = (snapshot.CE_GasSupplierContactSnapshots ?? Enumerable.Empty<CE_GasSupplierContactSnapshots>())
                    .Select(c => new SupplierContactSnapshotViewModel
                    {
                        Id = c.Id,
                        ContactName = c.ContactName,
                        Role = c.Role,
                        PhoneNumber = c.PhoneNumber,
                        Email = c.Email,
                        Notes = c.Notes
                    }).ToList()
            };
        }

        private SupplierSnapshotViewModel MapSnapshot(CE_ElectricSupplierSnapshots snapshot)
        {
            return new SupplierSnapshotViewModel
            {
                Id = snapshot.Id,
                SupplierId = snapshot.SupplierId,
                EId = snapshot.EId,
                SupplierName = snapshot.SupplierName,
                Link = snapshot.SupplierLink,
                Products = (snapshot.CE_ElectricSupplierProductSnapshots ?? Enumerable.Empty<CE_ElectricSupplierProductSnapshots>())
                    .Select(p => new SupplierProductSnapshotViewModel
                    {
                        Id = p.Id,
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        SupplierCommsType = p.SupplierCommsType,
                        StartDate = p.StartDate.ToString("dd/MM/yyyy"),
                        EndDate = p.EndDate.ToString("dd/MM/yyyy"),
                        Commission = p.Commission
                    }).ToList(),
                Uplifts = (snapshot.CE_ElectricSupplierUpliftSnapshots ?? Enumerable.Empty<CE_ElectricSupplierUpliftSnapshots>())
                    .Select(u => new SupplierUpliftSnapshotViewModel
                    {
                        Id = u.Id,
                        Uplift = u.Uplift,
                        FuelType = u.FuelType,
                        StartDate = u.StartDate,
                        EndDate = u.EndDate
                    }).ToList(),
                Contacts = (snapshot.CE_ElectricSupplierContactSnapshots ?? Enumerable.Empty<CE_ElectricSupplierContactSnapshots>())
                    .Select(c => new SupplierContactSnapshotViewModel
                    {
                        Id = c.Id,
                        ContactName = c.ContactName,
                        Role = c.Role,
                        PhoneNumber = c.PhoneNumber,
                        Email = c.Email,
                        Notes = c.Notes
                    }).ToList()
            };
        }


        private async Task<bool> UpdateContractEmail(UpdatePostSalesFieldDto dto)
        {
            if (dto.ContractType == "Electric")
            {
                var contract = await _db.CE_ElectricContracts
                    .FirstOrDefaultAsync(x => x.EId == dto.EId && x.Id == dto.ContractId);

                if (contract != null)
                {
                    contract.EmailAddress = dto.Email;
                    return true;
                }
            }
            else
            {
                var contract = await _db.CE_GasContracts
                    .FirstOrDefaultAsync(x => x.EId == dto.EId && x.Id == dto.ContractId);

                if (contract != null)
                {
                    contract.EmailAddress = dto.Email;
                    return true;
                }
            }

            return false;
        }

        private async Task<CE_CommissionAndReconciliation> UpsertReconciliation(UpdatePostSalesFieldDto dto)
        {
            var cr = await _db.CE_CommissionAndReconciliation
                .Include(x => x.Metrics)
                .FirstOrDefaultAsync(c => c.EId == dto.EId && c.contractType == dto.ContractType);

            if (cr == null)
            {
                cr = new CE_CommissionAndReconciliation
                {
                    EId = dto.EId,
                    contractType = dto.ContractType
                };
                _db.CE_CommissionAndReconciliation.Add(cr);
            }

            if (DateTime.TryParse(dto.StartDate, out var sd))
                cr.StartDate = sd.ToString("yyyy-MM-dd");

            if (DateTime.TryParse(dto.CED, out var ced))
                cr.CED = ced.ToString("yyyy-MM-dd");
            else if (!string.IsNullOrWhiteSpace(dto.StartDate) && int.TryParse(dto.Duration, out var durYears))
                cr.CED = sd.AddYears(durYears).AddDays(-1).ToString("yyyy-MM-dd");

            if (DateTime.TryParse(dto.COTDate, out var cot))
                cr.CED_COT = cot.ToString("yyyy-MM-dd");

            return cr;
        }

        private async Task<bool> UpsertMetrics(CE_CommissionAndReconciliation cr)
        {
            if (cr == null) return false;

            var metrics = await _db.CE_CommissionMetrics
                .FirstOrDefaultAsync(m => m.ReconciliationId == cr.Id && m.contractType == cr.contractType);

            if (metrics == null)
            {
                metrics = new CE_CommissionMetrics
                {
                    ReconciliationId = cr.Id,
                    contractType = cr.contractType
                };
                _db.CE_CommissionMetrics.Add(metrics);
                return true;
            }

            return false;
        }

        private async Task<bool> UpdateContractStatus(UpdatePostSalesFieldDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ContractStatus)) return false;

            var status = await _db.CE_ContractStatuses
                .FirstOrDefaultAsync(s => s.EId == dto.EId && s.Type == dto.ContractType);

            if (status != null)
            {
                status.ContractStatus = dto.ContractStatus;
                status.ModifyDate = DateTime.Now;
                return true;
            }

            return false;
        }

        private async Task<bool> UpsertObjection(UpdatePostSalesFieldDto dto)
        {
            if (dto.ObjectionDate == null && string.IsNullOrWhiteSpace(dto.QueryType))
                return false;

            var objection = await _db.CE_PostSaleObjections
                .FirstOrDefaultAsync(o => o.EId == dto.EId && o.ContractType == dto.ContractType);

            if (objection == null)
            {
                objection = new CE_PostSaleObjection
                {
                    EId = dto.EId,
                    ContractType = dto.ContractType,
                    QueryType = dto.QueryType,
                    ObjectionDate = dto.ObjectionDate,
                    ObjectionCount = dto.ObjectionDate == null ? 0 : 1,
                    ReAppliedDate = dto.ReAppliedDate,
                    ReAppliedCount = dto.ReAppliedDate == null ? 0 : 1,
                    CreatedBy = User.Identity?.Name ?? "System",
                    CreatedDate = DateTime.Now,
                    ModifyDate = DateTime.Now
                };

                _db.CE_PostSaleObjections.Add(objection);
                return true;
            }

            objection.ObjectionDate = dto.ObjectionDate;
            objection.QueryType = dto.QueryType;
            if (dto.ObjectionDate != null)
                objection.ObjectionCount += 1;
            if (dto.ReAppliedDate != null)
                objection.ReAppliedCount += 1;
            objection.ModifyDate = DateTime.Now;
            objection.ModifyBy = User.Identity?.Name ?? "System";

            return true;
        }

        private bool InsertStatusDashboardLog(UpdatePostSalesFieldDto dto, string userName)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.EId))
                return false;

            var log = new CE_PostSalesLogs
            {
                EId = dto.EId,
                ContractType = dto.ContractType,
                ContractStatus = dto.ContractStatus,
                CSD = dto.StartDate,
                CED = dto.CED,
                COT = dto.COTDate,
                ReAppliedDate = dto.ReAppliedDate,
                ObjectionDate = dto.ObjectionDate,
                CreatedBy = userName,
                CreatedDate = DateTime.Now
            };

            _db.CE_PostSalesLogs.Add(log);
            return true;
        }

        // GET POST SALES ROW

        private IQueryable<ContractViewModel> GetCombinedContracts()
        {
            var electric = from ec in _db.CE_ElectricContracts
                           join sp in _db.CE_Supplier on ec.SupplierId equals sp.Id
                           select new ContractViewModel
                           {
                               Id = ec.Id,
                               EId = ec.EId,
                               ContractType = "Electric",
                               BusinessName = ec.BusinessName,
                               SupplierId = ec.SupplierId,
                               SupplierName = sp.Name,
                               Department = ec.Department,
                               CloserId = ec.CloserId,
                               BrokerageId = ec.BrokerageId,
                               BrokerageStaffId = ec.BrokerageStaffId,
                               IntroducerId = ec.IntroducerId,
                               SubBrokerageId = ec.SubBrokerageId,
                               SubIntroducerId = ec.SubIntroducerId,
                               SubReferralPartnerId = ec.SubReferralPartnerId,
                               ReferralPartnerId = ec.ReferralPartnerId,
                               LeadGeneratorId = ec.LeadGeneratorId,
                               EmailAddress = ec.EmailAddress,
                               PostCode = ec.PostCode,
                               Duration = ec.Duration,
                               MPXN = ec.MPAN,
                               InputDate = ec.InputDate,
                               StartDate = ec.InitialStartDate,
                               ContractNotes = ec.ContractNotes,
                               Link = sp.Link
                           };

            var gas = from gc in _db.CE_GasContracts
                      join sp in _db.CE_Supplier on gc.SupplierId equals sp.Id
                      select new ContractViewModel
                      {
                          Id = gc.Id,
                          EId = gc.EId,
                          ContractType = "Gas",
                          BusinessName = gc.BusinessName,
                          SupplierId = gc.SupplierId,
                          SupplierName = sp.Name,
                          Department = gc.Department,
                          CloserId = gc.CloserId,
                          BrokerageId = gc.BrokerageId,
                          BrokerageStaffId = gc.BrokerageStaffId,
                          IntroducerId = gc.IntroducerId,
                          SubBrokerageId = gc.SubBrokerageId,
                          SubIntroducerId = gc.SubIntroducerId,
                          SubReferralPartnerId = gc.SubReferralPartnerId,
                          ReferralPartnerId = gc.ReferralPartnerId,
                          LeadGeneratorId = gc.LeadGeneratorId,
                          EmailAddress = gc.EmailAddress,
                          PostCode = gc.PostCode,
                          Duration = gc.Duration,
                          MPXN = gc.MPRN,
                          InputDate = gc.InputDate,
                          StartDate = gc.InitialStartDate,
                          ContractNotes = gc.ContractNotes,
                          Link = sp.Link
                      };

            return electric.Concat(gas);
        }

        private async Task<ContractViewModel> GetContractDetailsAsync(string eId, string type, CE_Accounts account)
        {
            var sortCodeConst = account?.SortCode ?? "";
            var accountNoConst = account?.AccountNumber ?? "";

            if (string.Equals(type, "Electric", StringComparison.OrdinalIgnoreCase))
            {
                return await (from ec in _db.CE_ElectricContracts
                                  join sp in _db.CE_Supplier on ec.SupplierId equals sp.Id
                                  where ec.EId == eId
                                  select new ContractViewModel
                                  {
                                      Id = ec.Id,
                                      EId = ec.EId,
                                      ContractType = "Electric",
                                      BusinessName = ec.BusinessName,
                                      SupplierId = ec.SupplierId,
                                      SupplierName = sp.Name,
                                      Department = ec.Department,
                                      CloserId = ec.CloserId,
                                      BrokerageId = ec.BrokerageId,
                                      BrokerageStaffId = ec.BrokerageStaffId,
                                      IntroducerId = ec.IntroducerId,
                                      SubBrokerageId = ec.SubBrokerageId,
                                      SubIntroducerId = ec.SubIntroducerId,
                                      SubReferralPartnerId = ec.SubReferralPartnerId,
                                      ReferralPartnerId = ec.ReferralPartnerId,
                                      LeadGeneratorId = ec.LeadGeneratorId,
                                      EmailAddress = ec.EmailAddress,
                                      PostCode = ec.PostCode,
                                      MPXN = ec.MPAN,
                                      InputDate = ec.InputDate,
                                      StartDate = ec.InitialStartDate,
                                      ContractNotes = ec.ContractNotes,
                                      Link = sp.Link,
                                      SupplierCommsType = ec.SupplierCommsType,
                                      ProductId = ec.ProductId,
                                      CustomerName = ec.CustomerName,
                                      BusinessDoorNumber = ec.BusinessDoorNumber,
                                      BusinessHouseName = ec.BusinessHouseName,
                                      BusinessStreet = ec.BusinessStreet,
                                      BusinessTown = ec.BusinessTown,
                                      BusinessCounty = ec.BusinessCounty,
                                      PhoneNumber1 = ec.PhoneNumber1,
                                      PhoneNumber2 = ec.PhoneNumber2,
                                      Uplift = ec.Uplift,
                                      Duration = ec.Duration,
                                      InputEAC = ec.InputEAC,
                                      //UnitRate = ec.UnitRate,
                                      OtherRate = ec.OtherRate,
                                      StandingCharge = ec.StandingCharge,
                                      SortCode = sortCodeConst,
                                      AccountNumber = accountNoConst,
                                      CurrentSupplier = ec.CurrentSupplier,
                                      EMProcessor = ec.EMProcessor,
                                      ContractChecked = ec.ContractChecked,
                                      ContractAudited = ec.ContractAudited,
                                      Terminated = ec.Terminated,
                                      Source = ec.Source,
                                      SalesType = ec.SalesType,
                                      SalesTypeStatus = ec.SalesTypeStatus,
                                      PreSalesStatus = ec.PreSalesStatus,
                                      TopLine = ec.TopLine,
                                      // Brokerage Details
                                      OfgemId = ec.OfgemId,
                                      // Dynamic Department-based fields
                                      Collaboration = ec.Collaboration,


                                  }).FirstOrDefaultAsync();

            }
            else
            {
                return await (from gc in _db.CE_GasContracts
                                  join sp in _db.CE_Supplier on gc.SupplierId equals sp.Id
                                  where gc.EId == eId
                                  select new ContractViewModel
                                  {
                                      Id = gc.Id,
                                      EId = gc.EId,
                                      ContractType = "Gas",
                                      BusinessName = gc.BusinessName,
                                      SupplierId = gc.SupplierId,
                                      SupplierName = sp.Name,
                                      Department = gc.Department,
                                      CloserId = gc.CloserId,
                                      BrokerageId = gc.BrokerageId,
                                      BrokerageStaffId = gc.BrokerageStaffId,
                                      IntroducerId = gc.IntroducerId,
                                      SubBrokerageId = gc.SubBrokerageId,
                                      SubIntroducerId = gc.SubIntroducerId,
                                      SubReferralPartnerId = gc.SubReferralPartnerId,
                                      ReferralPartnerId = gc.ReferralPartnerId,
                                      LeadGeneratorId = gc.LeadGeneratorId,
                                      EmailAddress = gc.EmailAddress,
                                      PostCode = gc.PostCode,
                                      MPXN = gc.MPRN,
                                      InputDate = gc.InputDate,
                                      StartDate = gc.InitialStartDate,
                                      ContractNotes = gc.ContractNotes,
                                      Link = sp.Link,
                                      SupplierCommsType = gc.SupplierCommsType,
                                      ProductId = gc.ProductId,
                                      CustomerName = gc.CustomerName,
                                      BusinessDoorNumber = gc.BusinessDoorNumber,
                                      BusinessHouseName = gc.BusinessHouseName,
                                      BusinessStreet = gc.BusinessStreet,
                                      BusinessTown = gc.BusinessTown,
                                      BusinessCounty = gc.BusinessCounty,
                                      PhoneNumber1 = gc.PhoneNumber1,
                                      PhoneNumber2 = gc.PhoneNumber2,
                                      Uplift = gc.Uplift,
                                      Duration = gc.Duration,
                                      InputEAC = gc.InputEAC,
                                      UnitRate = gc.UnitRate,
                                      OtherRate = gc.OtherRate,
                                      StandingCharge = gc.StandingCharge,
                                      SortCode = sortCodeConst,
                                      AccountNumber = accountNoConst,
                                      CurrentSupplier = gc.CurrentSupplier,
                                      EMProcessor = gc.EMProcessor,
                                      ContractChecked = gc.ContractChecked,
                                      ContractAudited = gc.ContractAudited,
                                      Terminated = gc.Terminated,
                                      Source = gc.Source,
                                      SalesType = gc.SalesType,
                                      SalesTypeStatus = gc.SalesTypeStatus,
                                      PreSalesStatus = gc.PreSalesStatus,
                                      // Brokerage Details
                                      OfgemId = gc.OfgemId,
                                      // Dynamic Department-based fields
                                      Collaboration = gc.Collaboration,

                                  }).FirstOrDefaultAsync();
            }
        }

        private IQueryable<ContractViewModel> ApplyFilters(IQueryable<ContractViewModel> combined, StatusQueryParams q)
        {
            if (!string.IsNullOrEmpty(q.Department))
                combined = combined.Where(x => x.Department == q.Department);

            if (!string.IsNullOrEmpty(q.Supplier))
            {
                if (int.TryParse(q.Supplier, out var sId))
                    combined = combined.Where(x => x.SupplierId == sId);
                else
                    combined = combined.Where(x => x.SupplierId.ToString() == q.Supplier);
            }

            if (!string.IsNullOrEmpty(q.BrokerageId) && int.TryParse(q.BrokerageId, out var bId))
                combined = combined.Where(x => x.BrokerageId == bId);

            if (!string.IsNullOrEmpty(q.StaffId) && int.TryParse(q.StaffId, out var stId))
                combined = combined.Where(x => x.BrokerageStaffId == stId);

            if (!string.IsNullOrEmpty(q.SubBrokerageId) && int.TryParse(q.SubBrokerageId, out var sbId))
                combined = combined.Where(x => x.SubBrokerageId == sbId);

            if (!string.IsNullOrEmpty(q.CloserId) && int.TryParse(q.CloserId, out var clId))
                combined = combined.Where(x => x.CloserId == clId);

            if (!string.IsNullOrEmpty(q.LeadGeneratorId) && int.TryParse(q.LeadGeneratorId, out var lgId))
                combined = combined.Where(x => x.LeadGeneratorId == lgId);

            if (!string.IsNullOrEmpty(q.ReferralPartnerId) && int.TryParse(q.ReferralPartnerId, out var rpId))
                combined = combined.Where(x => x.ReferralPartnerId == rpId);

            if (!string.IsNullOrEmpty(q.SubReferralPartnerId) && int.TryParse(q.SubReferralPartnerId, out var subRefId))
                combined = combined.Where(x => x.SubReferralPartnerId == subRefId);

            if (!string.IsNullOrEmpty(q.IntroducerId) && int.TryParse(q.IntroducerId, out var introId))
                combined = combined.Where(x => x.IntroducerId == introId);

            if (!string.IsNullOrEmpty(q.SubIntroducerId) && int.TryParse(q.SubIntroducerId, out var subIntroId))
                combined = combined.Where(x => x.SubIntroducerId == subIntroId);

            return combined;
        }

        private List<ContractViewModel> ApplySearching(List<ContractViewModel> contracts, string searchValue)
        {
            if (string.IsNullOrWhiteSpace(searchValue))
                return contracts;

            searchValue = searchValue.ToLower();

            return contracts.Where(x =>
                (x.BusinessName ?? "").ToLower().Contains(searchValue) ||
                (x.SupplierName ?? "").ToLower().Contains(searchValue) ||
                (x.MPXN ?? "").ToLower().Contains(searchValue) ||
                (x.EmailAddress ?? "").ToLower().Contains(searchValue) ||
                (x.PostCode ?? "").ToLower().Contains(searchValue) ||
                (x.Department ?? "").ToLower().Contains(searchValue) ||
                (x.ContractNotes ?? "").ToLower().Contains(searchValue)
            ).ToList();
        }

        private List<ContractViewModel> ApplySorting(List<ContractViewModel> contracts, StatusQueryParams q,
            List<CE_CommissionAndReconciliation> crs, List<CE_ContractStatuses> statuses, List<CE_PostSaleObjection> postSaleObj)
        {
            var sortColumn = q.Columns?[q.Order?[0].Column ?? 0].Data;
            var sortDir = q.Order?[0].Dir?.ToLower();

            Func<ContractViewModel, object> keySelector = x => null;

            switch (sortColumn)
            {
                case "SupplierName": keySelector = x => x.SupplierName; break;
                case "BusinessName": keySelector = x => x.BusinessName; break;
                case "MPXN": keySelector = x => x.MPXN; break;
                case "InputDate": keySelector = x => ParserHelper.ParseDateForSorting(x.InputDate); break;
                case "StartDate":
                    keySelector = x =>
                    {
                        var crDate = ParserHelper.ParseDateForSorting(
                            crs.FirstOrDefault(c => c.EId == x.EId)?.StartDate
                        );

                        if (crDate != DateTime.MinValue)
                            return crDate;
                        return ParserHelper.ParseDateForSorting(x.StartDate);
                    };
                    break;
                case "CED":
                    keySelector = x =>
                    {
                        var cr = crs.FirstOrDefault(c => c.EId == x.EId);
                        return HelperUtility.GetEffectiveCED(x, cr);
                    };
                    break;
                case "COTDate":
                    keySelector = x =>
                    {
                        var crDate = ParserHelper.ParseDateForSorting(
                            crs.FirstOrDefault(c => c.EId == x.EId)?.CED_COT
                        );

                        if (crDate != DateTime.MinValue)
                            return crDate;
                        return ParserHelper.ParseDateForSorting(x.CED);
                    };
                    break;
                case "ContractStatus": keySelector = x => statuses.FirstOrDefault(s => s.EId == x.EId)?.ContractStatus ?? ""; break;
                case "ObjectionDate":
                    keySelector = x =>
                        ParserHelper.ParseDateForSorting(
                            postSaleObj.FirstOrDefault(s => s.EId == x.EId)?.ObjectionDate
                        );
                    break;
                case "ObjectionCount": keySelector = x => postSaleObj.FirstOrDefault(s => s.EId == x.EId)?.ObjectionCount ?? 0; break;
                case "QueryType": keySelector = x => postSaleObj.FirstOrDefault(s => s.EId == x.EId)?.QueryType ?? ""; break;
                case "ContractNotes": keySelector = x => x.ContractNotes; break;
                default: keySelector = x => x.BusinessName; break; // fallback
            }

            return (sortDir == "desc")
                ? contracts.OrderByDescending(keySelector).ToList()
                : contracts.OrderBy(keySelector).ToList();
        }

        private List<PostSalesRowViewModel> ProjectRows(
            List<ContractViewModel> page,
            List<CE_ContractStatuses> statuses,
            List<CE_CommissionAndReconciliation> crs,
            List<CE_PostSaleObjection> postSaleObj,
            List<CE_SupplierContacts> supplierContacts)
        {
            var rows = new List<PostSalesRowViewModel>();

            foreach (var x in page)
            {
                var st = statuses.FirstOrDefault(s => s.EId == x.EId && s.Type == x.ContractType);
                var cr = crs.FirstOrDefault(c => c.EId == x.EId && c.contractType == x.ContractType);
                var postSales = postSaleObj.FirstOrDefault(p => p.EId == x.EId && p.ContractType == x.ContractType);
                var emailList = supplierContacts
                    .Where(sc => sc.SupplierId == x.SupplierId)
                    .Select(sc => sc.Email)
                    .Where(email => !string.IsNullOrWhiteSpace(email))
                    .Distinct()
                    .ToList();

                // Resolve Agent Name + Email
                var (agentName, agentEmail) = ResolveAgent(x);

                // Resolve Collaboration
                var collaborationName = ResolveCollaboration(x);

                // Supplier Email Template
                string queryType = postSales?.QueryType;
                var emailDetails = _db.CE_EmailTemplateLookups
                    .FirstOrDefault(e => e.SupplierId == x.SupplierId && e.QueryType == queryType);

                var effectiveCed = HelperUtility.GetEffectiveCED(x, cr);

                rows.Add(new PostSalesRowViewModel
                {
                    ContractId = x.Id,
                    EId = x.EId,
                    SupplierId = x.SupplierId,
                    Agent = agentName, // simplified for demo
                    AgentEmail = agentEmail,
                    Collaboration = collaborationName,
                    CobanaSalesType = x.Department,
                    MPXN = x.MPXN,
                    Duration = x.Duration,
                    SupplierName = x.SupplierName,
                    BusinessName = x.BusinessName,
                    InputDate = x.InputDate,
                    StartDate = cr?.StartDate ?? x.StartDate,
                    PostCode = x.PostCode ?? "-",
                    CED = effectiveCed.ToString("yyyy-MM-dd"),
                    COTDate = cr?.CED_COT,
                    Email = x.EmailAddress ?? "-",
                    ContractStatus = st?.ContractStatus ?? "-",
                    ObjectionCount = postSales?.ObjectionCount ?? 0,
                    ObjectionDate = postSales?.ObjectionDate,
                    QueryType = postSales?.QueryType ?? "-",
                    ContractType = x.ContractType,
                    EmailList = emailList,
                    EmailSubject = emailDetails?.Subject,
                    EmailBody = emailDetails?.EmailBody,
                    ContractNotes = x.ContractNotes,
                    Link = x.Link
                });
            }

            return rows;
        }

        private List<ContractStatusSummary> GetContractStatusSummary(
                     List<ContractViewModel> contracts,
                     List<CE_ContractStatuses> statuses)
        {
            var counterList = ContractStatusHelper.AllStatuses
                .ToDictionary(kv => kv.Key, kv => 0);

            foreach (var contract in contracts)
            {
                var matched = statuses.FirstOrDefault(s => s.EId == contract.EId && s.Type == contract.ContractType);
                var currentStatus = matched?.ContractStatus ?? "Unknown";

                if (counterList.ContainsKey(currentStatus))
                    counterList[currentStatus]++;
                else
                    counterList[currentStatus] = 1; // handle new/unknown statuses
            }

            // Step 3: Build summary list with color codes
            var summaryList = counterList
                .Select(x => new ContractStatusSummary
                {
                    ContractStatus = x.Key,
                    Count = x.Value,
                    ColorCode = ContractStatusHelper.AllStatuses.TryGetValue(x.Key, out var color)
                        ? color
                        : "#CCCCCC" // fallback for unknowns
                })
                .OrderByDescending(x => x.Count)
                .ToList();
            return summaryList;
        }


        private (string agentName, string agentEmail) ResolveAgent(ContractViewModel x)
        {
            string agentName = "-";
            string agentEmail = "-";

            if (x.Department == "In House" && x.CloserId.HasValue)
            {
                var closer = _db.CE_Sector
                    .Where(s => s.SectorID == x.CloserId && s.SectorType == "closer")
                    .Select(s => new { s.Name, s.Email })
                    .FirstOrDefault();

                agentName = closer?.Name ?? "-";
                agentEmail = closer?.Email ?? "-";
            }
            else if (x.Department == "Brokers" && x.BrokerageStaffId.HasValue)
            {
                var staff = _db.CE_BrokerageStaff
                    .Where(bs => bs.BrokerageStaffID == x.BrokerageStaffId)
                    .Select(bs => new { bs.BrokerageStaffName, bs.Email })
                    .FirstOrDefault();

                agentName = staff?.BrokerageStaffName ?? "-";
                agentEmail = staff?.Email ?? "-";
            }
            else if (x.Department == "Introducers" && x.SubIntroducerId.HasValue)
            {
                var intro = _db.CE_SubIntroducer
                    .Where(si => si.SubIntroducerID == x.SubIntroducerId)
                    .Select(si => new { si.SubIntroducerName, si.SubIntroducerEmail })
                    .FirstOrDefault();

                agentName = intro?.SubIntroducerName ?? "-";
                agentEmail = intro?.SubIntroducerEmail ?? "-";
            }

            return (agentName, agentEmail);
        }

        private string ResolveCollaboration(ContractViewModel x)
        {
            string collaborationName = "-";

            if (x.Department == "In House")
            {
                if (x.LeadGeneratorId.HasValue)
                {
                    collaborationName = _db.CE_Sector
                        .Where(lg => lg.SectorID == x.LeadGeneratorId && lg.SectorType == "Leads Generator")
                        .Select(lg => lg.Name)
                        .FirstOrDefault() ?? "-";
                }
                if (x.ReferralPartnerId.HasValue)
                {
                    collaborationName = _db.CE_Sector
                        .Where(rp => rp.SectorID == x.ReferralPartnerId && rp.SectorType == "Referral Partner")
                        .Select(rp => rp.Name)
                        .FirstOrDefault() ?? "-";
                }
            }
            else if (x.Department == "Brokers" && x.SubBrokerageId.HasValue)
            {
                collaborationName = _db.CE_SubBrokerage
                    .Where(sb => sb.SubBrokerageID == x.SubBrokerageId)
                    .Select(sb => sb.SubBrokerageName)
                    .FirstOrDefault() ?? "-";
            }
            else if (x.Department == "Introducers" && x.SubIntroducerId.HasValue)
            {
                collaborationName = _db.CE_SubIntroducer
                    .Where(si => si.SubIntroducerID == x.SubIntroducerId)
                    .Select(si => si.SubIntroducerName)
                    .FirstOrDefault() ?? "-";
            }

            return collaborationName;
        }

        #endregion
    }
}