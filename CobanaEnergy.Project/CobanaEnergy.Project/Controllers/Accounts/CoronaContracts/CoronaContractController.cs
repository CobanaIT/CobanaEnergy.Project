using CobanaEnergy.Project.Common;
using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Helpers;
using CobanaEnergy.Project.Filters;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.Accounts;
using CobanaEnergy.Project.Models.Accounts.SuppliersModels;
using CobanaEnergy.Project.Models.Accounts.SuppliersModels.BGB;
using CobanaEnergy.Project.Models.Accounts.SuppliersModels.BGB.DBModel;
using CobanaEnergy.Project.Models.Accounts.SuppliersModels.BGLite;
using CobanaEnergy.Project.Models.Accounts.SuppliersModels.Corona;
using Logic;
using Logic.ResponseModel.Helper;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using static NPOI.HSSF.Util.HSSFColor;

namespace CobanaEnergy.Project.Controllers.Accounts.CoronaContracts
{
    public class CoronaContractController : BaseController
    {
        private readonly ApplicationDBContext _db;
        public CoronaContractController(ApplicationDBContext db)
        {
            _db = db;
        }

        #region CoronaContract 

        [HttpGet]
        [Authorize(Roles = "Accounts,Controls")]
        public async Task<ActionResult> EditCoronaContract(string id, string supplierId, string type)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id) || !Guid.TryParse(id, out _) ||
                    string.IsNullOrWhiteSpace(supplierId) || !int.TryParse(supplierId, out _) ||
                    string.IsNullOrWhiteSpace(type))
                {
                    return HttpNotFound("Invalid ID, SupplierId, or Type.");
                }

                var model = new EditCoronaContractViewModel
                {
                    Id = id,
                    SupplierId = supplierId
                };

                if (Regex.IsMatch(type, @"^\d{13}$")) // ---- Electric Section ----
                {
                    var electricContract = await _db.CE_ElectricContracts.AsNoTracking()
                        .FirstOrDefaultAsync(e => e.EId == id && e.MPAN == type);

                    if (electricContract == null)
                        return HttpNotFound("Electric contract not found.");

                    model.Department = electricContract.Department;
                    model.BusinessName = electricContract.BusinessName;
                    model.SalesTypeElectric = electricContract.SalesType;
                    model.MPAN = electricContract.MPAN;
                    model.DurationElectric = electricContract.Duration;
                    model.UpliftElectric = electricContract.Uplift?.ToString();
                    model.ContractNotes = electricContract.ContractNotes;
                    model.HasElectricDetails = true;

                    if (!string.IsNullOrWhiteSpace(electricContract.InputDate) &&
                        DateTime.TryParse(electricContract.InputDate, out DateTime parsedInputDate))
                    {
                        model.InputDateElectric = parsedInputDate.ToString("yyyy-MM-dd");
                    }

                    var snapshot = await _db.CE_ElectricSupplierSnapshots
                        .FirstOrDefaultAsync(s => s.EId == id && s.SupplierId == electricContract.SupplierId);

                    if (snapshot != null)
                    {
                        long snapshotId = snapshot.Id;

                        var productSnapshots = await _db.CE_ElectricSupplierProductSnapshots
                            .Where(p => p.SnapshotId == snapshotId && p.SupplierId == snapshot.SupplierId)
                            .ToListAsync();

                        model.ProductElectricList = productSnapshots
                            .Select(p => new SelectListItem
                            {
                                Value = p.ProductId.ToString(),
                                Text = p.ProductName
                            })
                            .ToList();

                        var matchedProduct = productSnapshots
                            .FirstOrDefault(p => p.ProductId == electricContract.ProductId);
                        if (matchedProduct != null)
                        {
                            model.SelectedProductElectric = matchedProduct.ProductId;
                            model.SupplierCommsTypeElectric = matchedProduct.SupplierCommsType;
                            model.CommissionElectric = matchedProduct.Commission?.ToString();
                        }
                    }

                    var commissionReconciliation = await _db.CE_CommissionAndReconciliation
                    .FirstOrDefaultAsync(x => x.EId == id);

                    if (commissionReconciliation != null)
                    {
                        model.InitialStartDate = commissionReconciliation.StartDate;
                        model.CED = commissionReconciliation.CED;
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(electricContract.InitialStartDate) &&
                        DateTime.TryParse(electricContract.InitialStartDate, out DateTime parsedStartDate))
                        {
                            model.InitialStartDate = parsedStartDate.ToString("yyyy-MM-dd");

                            if (!string.IsNullOrWhiteSpace(electricContract.Duration) &&
                                int.TryParse(electricContract.Duration, out int durationYears))
                            {
                                var cedDate = parsedStartDate.AddYears(durationYears).AddDays(-1);
                                model.CED = cedDate.ToString("yyyy-MM-dd");
                            }
                        }
                    }

                    await ReconciliationAndCommsssionMetrics(id, model, "Electric");
                    model.PaymentNoteLogs = await _db.CE_PaymentAndNoteLogs
                                        .Where(x => x.EId == id && x.contracttype == "Electric")
                                        .OrderByDescending(x => x.CreatedAt)
                                        .ToListAsync();

                    // Populate department-based fields for Electric
                    var departmentFields = new Dictionary<string, string>();
                    await ParserHelper.PopulateDepartmentFieldsForElectric(electricContract, departmentFields, _db);
                    
                    // Map dictionary values to model properties
                    model.BrokerageStaffName = departmentFields.ContainsKey("BrokerageStaffName") ? departmentFields["BrokerageStaffName"] : "N/A";
                    model.SubBrokerageName = departmentFields.ContainsKey("SubBrokerageName") ? departmentFields["SubBrokerageName"] : "N/A";
                    model.CloserName = departmentFields.ContainsKey("CloserName") ? departmentFields["CloserName"] : "N/A";
                    model.LeadGeneratorName = departmentFields.ContainsKey("LeadGeneratorName") ? departmentFields["LeadGeneratorName"] : "N/A";
                    model.ReferralPartnerName = departmentFields.ContainsKey("ReferralPartnerName") ? departmentFields["ReferralPartnerName"] : "N/A";
                    model.SubReferralPartnerName = departmentFields.ContainsKey("SubReferralPartnerName") ? departmentFields["SubReferralPartnerName"] : "N/A";
                    model.IntroducerName = departmentFields.ContainsKey("IntroducerName") ? departmentFields["IntroducerName"] : "N/A";
                    model.SubIntroducerName = departmentFields.ContainsKey("SubIntroducerName") ? departmentFields["SubIntroducerName"] : "N/A";
                }
                else if (Regex.IsMatch(type, @"^\d{6,10}$")) // ---- Gas Section ----
                {
                    var gasContract = await _db.CE_GasContracts.AsNoTracking()
                        .FirstOrDefaultAsync(g => g.EId == id && g.MPRN == type);

                    if (gasContract == null)
                        return HttpNotFound("Gas contract not found.");

                    model.Department = gasContract.Department;
                    model.BusinessName = gasContract.BusinessName;
                    model.SalesTypeGas = gasContract.SalesType;
                    model.MPRN = gasContract.MPRN;
                    model.DurationGas = gasContract.Duration;
                    model.UpliftGas = gasContract.Uplift?.ToString();
                    model.ContractNotes = gasContract.ContractNotes;
                    model.HasGasDetails = true;

                    if (!string.IsNullOrWhiteSpace(gasContract.InputDate) &&
                        DateTime.TryParse(gasContract.InputDate, out DateTime parsedInputDate))
                    {
                        model.InputDateGas = parsedInputDate.ToString("yyyy-MM-dd");
                    }

                    var snapshot = await _db.CE_GasSupplierSnapshots
                        .FirstOrDefaultAsync(s => s.EId == id && s.SupplierId == gasContract.SupplierId);

                    if (snapshot != null)
                    {
                        long snapshotId = snapshot.Id;

                        var productSnapshots = await _db.CE_GasSupplierProductSnapshots
                            .Where(p => p.SnapshotId == snapshotId && p.SupplierId == snapshot.SupplierId)
                            .ToListAsync();

                        model.ProductGasList = productSnapshots
                            .Select(p => new SelectListItem
                            {
                                Value = p.ProductId.ToString(),
                                Text = p.ProductName
                            })
                            .ToList();

                        var matchedProduct = productSnapshots
                            .FirstOrDefault(p => p.ProductId == gasContract.ProductId);
                        if (matchedProduct != null)
                        {
                            model.SelectedProductGas = matchedProduct.ProductId;
                            model.SupplierCommsTypeGas = matchedProduct.SupplierCommsType;
                            model.CommissionGas = matchedProduct.Commission?.ToString();
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(gasContract.InitialStartDate) &&
                        DateTime.TryParse(gasContract.InitialStartDate, out DateTime parsedStartDate))
                    {
                        model.InitialStartDate = parsedStartDate.ToString("yyyy-MM-dd");

                        if (!string.IsNullOrWhiteSpace(gasContract.Duration) &&
                            int.TryParse(gasContract.Duration, out int durationYears))
                        {
                            var cedDate = parsedStartDate.AddYears(durationYears).AddDays(-1);
                            model.CED = cedDate.ToString("yyyy-MM-dd");
                        }
                    }
                    await ReconciliationAndCommsssionMetrics(id, model, "Gas");
                    model.PaymentNoteLogs = await _db.CE_PaymentAndNoteLogs
                                        .Where(x => x.EId == id && x.contracttype == "Gas")
                                        .OrderByDescending(x => x.CreatedAt)
                                        .ToListAsync();

                    // Populate department-based fields for Gas
                    var departmentFields = new Dictionary<string, string>();
                    await ParserHelper.PopulateDepartmentFieldsForGas(gasContract, departmentFields, _db);
                    
                    // Map dictionary values to model properties
                    model.BrokerageStaffName = departmentFields.ContainsKey("BrokerageStaffName") ? departmentFields["BrokerageStaffName"] : "N/A";
                    model.SubBrokerageName = departmentFields.ContainsKey("SubBrokerageName") ? departmentFields["SubBrokerageName"] : "N/A";
                    model.CloserName = departmentFields.ContainsKey("CloserName") ? departmentFields["CloserName"] : "N/A";
                    model.LeadGeneratorName = departmentFields.ContainsKey("LeadGeneratorName") ? departmentFields["LeadGeneratorName"] : "N/A";
                    model.ReferralPartnerName = departmentFields.ContainsKey("ReferralPartnerName") ? departmentFields["ReferralPartnerName"] : "N/A";
                    model.SubReferralPartnerName = departmentFields.ContainsKey("SubReferralPartnerName") ? departmentFields["SubReferralPartnerName"] : "N/A";
                    model.IntroducerName = departmentFields.ContainsKey("IntroducerName") ? departmentFields["IntroducerName"] : "N/A";
                    model.SubIntroducerName = departmentFields.ContainsKey("SubIntroducerName") ? departmentFields["SubIntroducerName"] : "N/A";
                }
                else
                {
                    return HttpNotFound("Unknown contract type.");
                }



                if (int.TryParse(supplierId, out int supId))
                {
                    var latestUpload = await _db.CE_InvoiceSupplierUploads
                        .Where(x => x.SupplierId == supId)
                        .OrderByDescending(x => x.UploadedOn)
                        .ThenByDescending(x => x.Id)
                        .Select(x => new { x.FileName, x.UploadedOn })
                        .FirstOrDefaultAsync();

                    if (latestUpload != null && !string.IsNullOrWhiteSpace(latestUpload.FileName))
                    {
                        var fileName = Path.GetFileNameWithoutExtension(latestUpload.FileName);
                        var parts = fileName.Split('_');

                        if (parts.Length == 2 &&
                            !string.IsNullOrWhiteSpace(parts[0]) &&
                            DateTime.TryParseExact(parts[1], "dd.MM.yyyy",
                                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime invoiceDate))
                        {
                            model.InvoiceNo = parts[0];
                            model.InvoiceDate = invoiceDate.ToString("yyyy-MM-dd");
                        }
                    }
                }

                model.PaymentDate = CalculatePaymentDate(model.InvoiceDate);

                return View("~/Views/Accounts/CoronaContract/EditCoronaContract.cshtml", model);
            }
            catch (Exception ex)
            {
                Logger.Log($"EditCoronaContract failed for id={id}, supplierId={supplierId}, type={type}: {ex}");
                return RedirectToAction("NotFound", "Error");
            }
        }
        private async Task ReconciliationAndCommsssionMetrics(string id, EditCoronaContractViewModel model, string contractType)
        {
            var reconciliation = await _db.CE_CommissionAndReconciliation
                .AsNoTracking()
                .Where(r => r.EId == id)
                .ToListAsync();

            var rec = reconciliation.FirstOrDefault(r => r.contractType == contractType);
            if (rec == null) return;

            model.OtherAmount = rec.OtherAmount;
            model.CedCOT = rec.CED_COT;
            model.CotLostConsumption = rec.COTLostConsumption;
            model.CobanaDueCommission = rec.CobanaDueCommission;
            model.CobanaFinalReconciliation = rec.CobanaFinalReconciliation;
            model.CommissionFollowUpDate = rec.CommissionFollowUpDate;
            model.SupplierCobanaInvoiceNotes = rec.SupplierCobanaInvoiceNotes;

            var metrics = await _db.CE_CommissionMetrics
                .AsNoTracking()
                .Where(m => m.ReconciliationId == rec.Id)
                .ToListAsync();

            var met = metrics.FirstOrDefault(m => m.contractType == contractType);
            if (met == null) return;

            model.ContractDurationDays = met.ContractDurationDays;
            model.LiveDays = met.LiveDays;
            model.PercentLiveDays = met.PercentLiveDays;
            model.TotalCommissionForecast = met.TotalCommissionForecast;
            model.InitialCommissionForecast = met.InitialCommissionForecast;
            model.COTLostReconciliation = met.COTLostReconciliation;
            model.TotalAverageEAC = met.TotalAverageEAC;

            var contractStatus = await _db.CE_ContractStatuses
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.EId == id && s.Type == contractType);

            if (contractStatus != null)
            {
                model.ContractStatus = contractStatus.ContractStatus;
                model.PaymentStatus = contractStatus.PaymentStatus;
            }
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        [Authorize(Roles = "Accounts,Controls")]
        public async Task<JsonResult> UpdateContract(UpdateContractViewModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.EId))
                return JsonResponse.Fail("Invalid data submitted.");

            using (var transaction = _db.Database.BeginTransaction())
            {
                try
                {
                    var electricContract = await _db.CE_ElectricContracts
                        .FirstOrDefaultAsync(c => c.EId == model.EId);

                    var gasContract = await _db.CE_GasContracts
                        .FirstOrDefaultAsync(c => c.EId == model.EId);

                    // ----- Electric -----
                    if (electricContract != null)
                    {
                        if (model.HasElectricDetails)
                        {
                            if (!(model.SupplierCommsTypeElectric.Equals("RESIDUAL", StringComparison.OrdinalIgnoreCase) || model.SupplierCommsTypeElectric.Equals("DURATION", StringComparison.OrdinalIgnoreCase)))
                                return JsonResponse.Fail("Invalid Supplier Comms Type for Corona. Only RESIDUAL or DURATION are allowed.");


                            electricContract.Uplift = model.UpliftElectric;
                            electricContract.SupplierCommsType = model.SupplierCommsTypeElectric;

                            var electricSnapshot = await _db.CE_ElectricSupplierSnapshots
                                .FirstOrDefaultAsync(s => s.EId == model.EId && s.SupplierId == electricContract.SupplierId);

                            if (electricSnapshot != null)
                            {
                                var productSnapshot = await _db.CE_ElectricSupplierProductSnapshots
                                    .FirstOrDefaultAsync(p => p.SnapshotId == electricSnapshot.Id
                                                           && p.ProductId == electricContract.ProductId);
                                if (productSnapshot != null)
                                {
                                    productSnapshot.SupplierCommsType = model.SupplierCommsTypeElectric;
                                }
                            }

                            // Insert or Update Contract Status & Payment Status for Electric
                            var electricStatus = await _db.CE_ContractStatuses
                                .FirstOrDefaultAsync(s => s.EId == model.EId && s.Type == "Electric");

                            if (electricStatus != null)
                            {
                                electricStatus.ContractStatus = model.contractStatus;
                                electricStatus.PaymentStatus = model.paymentStatus;
                                electricStatus.ModifyDate = DateTime.Now;
                            }
                            else
                            {
                                _db.CE_ContractStatuses.Add(new CE_ContractStatuses
                                {
                                    EId = model.EId,
                                    Type = "Electric",
                                    ContractStatus = model.contractStatus,
                                    PaymentStatus = model.paymentStatus,
                                    PostSalesCreationDate = DateTime.Now,
                                    ModifyDate = DateTime.Now
                                });
                            }
                            await SaveOrUpdateCommissionData(model, "Electric");
                        }

                        electricContract.ContractNotes = model.ContractNotes;
                        PaymentAndNotesLogs(model, "Electric");
                    }

                    // ----- Gas -----
                    if (gasContract != null)
                    {
                        if (model.HasGasDetails)
                        {

                            if (!(model.SupplierCommsTypeGas.Equals("RESIDUAL", StringComparison.OrdinalIgnoreCase) || model.SupplierCommsTypeGas.Equals("DURATION", StringComparison.OrdinalIgnoreCase)))
                                return JsonResponse.Fail("Invalid Supplier Comms Type for Corona. Only RESIDUAL or DURATION are allowed.");

                            gasContract.Uplift = model.UpliftGas;
                            gasContract.SupplierCommsType = model.SupplierCommsTypeGas;

                            var gasSnapshot = await _db.CE_GasSupplierSnapshots
                                .FirstOrDefaultAsync(s => s.EId == model.EId && s.SupplierId == gasContract.SupplierId);

                            if (gasSnapshot != null)
                            {
                                var productSnapshot = await _db.CE_GasSupplierProductSnapshots
                                    .FirstOrDefaultAsync(p => p.SnapshotId == gasSnapshot.Id
                                                           && p.ProductId == gasContract.ProductId);
                                if (productSnapshot != null)
                                {
                                    productSnapshot.SupplierCommsType = model.SupplierCommsTypeGas;
                                }
                            }

                            // Insert or Update Contract Status & Payment Status for Gas
                            var gasStatus = await _db.CE_ContractStatuses
                                .FirstOrDefaultAsync(s => s.EId == model.EId && s.Type == "Gas");

                            if (gasStatus != null)
                            {
                                gasStatus.ContractStatus = model.contractStatus;
                                gasStatus.PaymentStatus = model.paymentStatus;
                                gasStatus.ModifyDate = DateTime.Now;
                            }
                            else
                            {
                                _db.CE_ContractStatuses.Add(new CE_ContractStatuses
                                {
                                    EId = model.EId,
                                    Type = "Gas",
                                    ContractStatus = model.contractStatus,
                                    PaymentStatus = model.paymentStatus,
                                    PostSalesCreationDate = DateTime.Now,
                                    ModifyDate = DateTime.Now
                                });
                            }
                            await SaveOrUpdateCommissionData(model, "Gas");
                        }

                        gasContract.ContractNotes = model.ContractNotes;
                        PaymentAndNotesLogs(model, "Gas");
                    }
                    await _db.SaveChangesAsync().ConfigureAwait(false);
                    transaction.Commit();

                    return JsonResponse.Ok(null, "Contract updated successfully.");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Logger.Log("UpdateContract failed: " + ex);
                    return JsonResponse.Fail("An error occurred while updating the contract.");
                }
            }
        }

        private void PaymentAndNotesLogs(UpdateContractViewModel model, string contracttype)
        {
            var notesModel = new PaymentAndNotesLogsViewModel
            {
                CobanaInvoiceNotes = model.SupplierCobanaInvoiceNotes,
                PaymentStatus = model.paymentStatus,
                EId = model.EId,
                ContractType = contracttype,
                Dashboard = "CoronaContracts",
                Username = User?.Identity?.Name ?? "Unknown User"
            };
            PaymentLogsHelper.InsertPaymentAndNotesLogs(_db, notesModel);
        }

        private async Task SaveOrUpdateCommissionData(UpdateContractViewModel model, string contractType)
        {
            try
            {
                dynamic contract = null;
                var durationVal = string.Empty;
                if (contractType == "Electric")
                {
                    contract = await _db.CE_ElectricContracts
                        .FirstOrDefaultAsync(c => c.EId == model.EId);

                    contract.Duration = model.DurationElectric;
                    durationVal = model.DurationElectric;

                    _db.SaveChanges();
                }
                else if (contractType == "Gas")
                {
                    contract = await _db.CE_GasContracts
                        .FirstOrDefaultAsync(c => c.EId == model.EId);
                    contract.Duration = model.DurationElectric;
                    durationVal = model.DurationGas;
                    _db.SaveChanges();
                }

                if (contract == null)
                {
                    Logger.Log($"No contract found for EId={model.EId}, Type={contractType}");
                    return;
                }

                string uplift = (contractType == "Electric") ? model.UpliftElectric : model.UpliftGas;
                string supplierCommsType = (contractType == "Electric") ? model.SupplierCommsTypeElectric : model.SupplierCommsTypeGas;
                string commission = (contractType == "Electric") ? model.CommissionElectric : model.CommissionGas;

                if (supplierCommsType?.Trim().ToLower() == "residual")
                {

                    decimal.TryParse(uplift, out decimal upliftVal);
                    decimal.TryParse(commission, out decimal supplierCommsVal);

                    var eacLogs = await _db.CE_EacLogs
                        .Where(l => l.EId == model.EId && l.ContractType == contractType)
                        .OrderByDescending(l => l.CreatedAt)
                        .ToListAsync();

                    decimal resultantDuration = decimal.TryParse(durationVal, out decimal d) ? d : 1m;
                    var TotalEac = GetTotalEacValue(eacLogs, resultantDuration);

                    var reconciliation = await _db.CE_CommissionAndReconciliation
                        .FirstOrDefaultAsync(r => r.EId == model.EId && r.contractType == contractType);

                    if (reconciliation == null)
                    {
                        reconciliation = new CE_CommissionAndReconciliation
                        {
                            EId = model.EId,
                            contractType = contractType
                        };
                        _db.CE_CommissionAndReconciliation.Add(reconciliation);
                    }

                    reconciliation.OtherAmount = model.OtherAmount;
                    reconciliation.StartDate = model.StartDate;
                    reconciliation.CED = model.Ced;
                    reconciliation.CED_COT = model.CedCOT;
                    reconciliation.COTLostConsumption = model.CotLostConsumption;
                    reconciliation.CommissionFollowUpDate = model.CommissionFollowUpDate;
                    reconciliation.SupplierCobanaInvoiceNotes = model.SupplierCobanaInvoiceNotes;

                    var metrics = await _db.CE_CommissionMetrics
                        .FirstOrDefaultAsync(m => m.ReconciliationId == reconciliation.Id && m.contractType == contractType);

                    string contractDurationDays = "";
                    if (DateTime.TryParse(model.StartDate, out DateTime startDate) &&
                        DateTime.TryParse(model.Ced, out DateTime cedDate))
                    {
                        contractDurationDays = (cedDate - startDate).TotalDays.ToString("F5");
                    }

                    string liveDays = "", percentLiveDays = "", cotLostReconciliation = "", supplierEacFinal = "";

                    if (!string.IsNullOrWhiteSpace(model.CedCOT) &&
                        DateTime.TryParse(model.StartDate, out DateTime startDt) &&
                        DateTime.TryParse(model.CedCOT, out DateTime cedCOTDate))
                    {
                        liveDays = (cedCOTDate - startDt).TotalDays.ToString("F5");

                        if (decimal.TryParse(model.CotLostConsumption, out decimal cotLostVal))
                        {
                            cotLostReconciliation = (cotLostVal * upliftVal).ToString("F5"); // same --
                        }

                        foreach (var log in eacLogs)
                        {
                            log.FinalEac = cotLostReconciliation;
                            supplierEacFinal = log.FinalEac;
                        }
                        if (decimal.TryParse(model.CotLostConsumption, out decimal cotLostConVal) &&
                            upliftVal != 0)
                        {
                            /// var tAverage = ((cotLostConVal / liveDays) * 365).ToString("F2");
                            reconciliation.CobanaDueCommission = (cotLostConVal * upliftVal).ToString("F5"); // ([Total AVG EAC]/365) * Live Days * Uplift
                        }
                    }
                    else
                    {
                        reconciliation.CobanaDueCommission = (TotalEac * upliftVal).ToString("F5");
                    }

                    string totalCommissionForecast = "";
                    if (decimal.TryParse(contract.InputEAC, out decimal inputEACVal))
                    {
                        totalCommissionForecast = ((inputEACVal * upliftVal) / 12).ToString("F5");
                    }

                    string initialCommissionForecast = "";
                    if (decimal.TryParse(totalCommissionForecast, out decimal totalCommissionVal))
                    {
                        initialCommissionForecast = (totalCommissionVal * supplierCommsVal).ToString("F5");
                    }

                    decimal cobanaDue = decimal.TryParse(reconciliation.CobanaDueCommission, out decimal due) ? due : 0;
                    decimal otherAmount = decimal.TryParse(reconciliation.OtherAmount, out decimal other) ? other : 0;

                    var invoiceTotal = eacLogs
                        .Sum(x => decimal.TryParse(x.InvoiceAmount, out decimal inv) ? inv : 0);

                    var finalReconciliation = (cobanaDue + otherAmount - invoiceTotal).ToString("F5");
                    reconciliation.CobanaFinalReconciliation = finalReconciliation;

                    string totalAverageEAC = "";
                    if (!string.IsNullOrWhiteSpace(model.CedCOT))
                    {
                        if (decimal.TryParse(model.CotLostConsumption, out decimal cotLostVal) &&
                            decimal.TryParse(liveDays, out decimal live) && live != 0)
                        {
                            totalAverageEAC = ((cotLostVal / live) * 365).ToString("F2"); // >> (COT/LOST Consumption/Live Days) *365
                        }
                    }
                    else
                    {
                        totalAverageEAC = (TotalEac / resultantDuration).ToString("F2");
                    }

                    if (metrics != null)
                    {
                        metrics.ContractDurationDays = contractDurationDays;
                        metrics.LiveDays = liveDays;
                        metrics.PercentLiveDays = percentLiveDays;
                        metrics.TotalCommissionForecast = totalCommissionForecast;
                        metrics.InitialCommissionForecast = initialCommissionForecast;
                        metrics.COTLostReconciliation = cotLostReconciliation;
                        metrics.TotalAverageEAC = totalAverageEAC;
                    }
                    else
                    {
                        _db.CE_CommissionMetrics.Add(new CE_CommissionMetrics
                        {
                            ReconciliationId = reconciliation.Id,
                            ContractDurationDays = contractDurationDays,
                            LiveDays = liveDays,
                            PercentLiveDays = percentLiveDays,
                            TotalCommissionForecast = totalCommissionForecast,
                            InitialCommissionForecast = initialCommissionForecast,
                            COTLostReconciliation = cotLostReconciliation,
                            TotalAverageEAC = totalAverageEAC,
                            contractType = contractType
                        });
                    }

                }
                else
                {
                    #region [DURATION]

                    decimal.TryParse(uplift, out decimal upliftVal);
                    decimal.TryParse(commission, out decimal supplierCommsVal);

                    var eacLogs = await _db.CE_EacLogs
                        .Where(l => l.EId == model.EId && l.ContractType == contractType)
                        .OrderByDescending(l => l.CreatedAt)
                        .ToListAsync();

                    var year1Data = GetLatestEac(eacLogs, "1ST YEAR EAC-FINAL", "1ST YEAR EAC-INITIAL");
                    var year2Data = GetLatestEac(eacLogs, "2ND YEAR EAC-FINAL", "2ND YEAR EAC-INITIAL");
                    var year3Data = GetLatestEac(eacLogs, "3RD YEAR EAC-FINAL", "3RD YEAR EAC-INITIAL");
                    var year4Data = GetLatestEac(eacLogs, "4TH YEAR EAC-FINAL", "4TH YEAR EAC-INITIAL");
                    var year5Data = GetLatestEac(eacLogs, "5TH YEAR EAC-FINAL", "5TH YEAR EAC-INITIAL");

                    var reconciliation = await _db.CE_CommissionAndReconciliation
                        .FirstOrDefaultAsync(r => r.EId == model.EId && r.contractType == contractType);

                    if (reconciliation == null)
                    {
                        reconciliation = new CE_CommissionAndReconciliation
                        {
                            EId = model.EId,
                            contractType = contractType
                        };
                        _db.CE_CommissionAndReconciliation.Add(reconciliation);
                    }

                    reconciliation.OtherAmount = model.OtherAmount;
                    reconciliation.StartDate = model.StartDate;
                    reconciliation.CED = model.Ced;
                    reconciliation.CED_COT = model.CedCOT;
                    reconciliation.COTLostConsumption = model.CotLostConsumption;
                    reconciliation.CommissionFollowUpDate = model.CommissionFollowUpDate;
                    reconciliation.SupplierCobanaInvoiceNotes = model.SupplierCobanaInvoiceNotes;

                    var metrics = await _db.CE_CommissionMetrics
                        .FirstOrDefaultAsync(m => m.ReconciliationId == reconciliation.Id && m.contractType == contractType);

                    string contractDurationDays = "";
                    if (DateTime.TryParse(model.StartDate, out DateTime startDate) &&
                        DateTime.TryParse(model.Ced, out DateTime cedDate))
                    {
                        contractDurationDays = (cedDate - startDate).TotalDays.ToString("F5");
                    }

                    string liveDays = "", percentLiveDays = "", cotLostReconciliation = "", supplierEacFinal = "";

                    if (!string.IsNullOrWhiteSpace(model.CedCOT) &&
                        DateTime.TryParse(model.StartDate, out DateTime startDt) &&
                        DateTime.TryParse(model.CedCOT, out DateTime cedCOTDate))
                    {
                        liveDays = (cedCOTDate - startDt).TotalDays.ToString("F5");

                        if (decimal.TryParse(liveDays, out decimal live) &&
                            decimal.TryParse(contractDurationDays, out decimal duration) && duration != 0)
                        {
                            percentLiveDays = (live / duration).ToString("F5");
                        }

                        if (decimal.TryParse(model.CotLostConsumption, out decimal cotLostVal) && live != 0)
                        {
                            cotLostReconciliation = ((cotLostVal / live) * 365).ToString("F5");
                        }

                        foreach (var log in eacLogs)
                        {
                            log.FinalEac = cotLostReconciliation;
                            supplierEacFinal = log.FinalEac;
                        }

                        if (decimal.TryParse(supplierEacFinal, out decimal supplierEacFinalVal) &&
                            upliftVal != 0 && supplierCommsVal != 0 && live != 0)
                        {
                            reconciliation.CobanaDueCommission = ((supplierEacFinalVal * upliftVal * supplierCommsVal * live) / 365).ToString("F5");
                        }
                    }
                    else
                    {
                        int duration = int.TryParse(contract?.Duration, out int d) ? d : 1;
                        decimal dueCommission = 0m;

                        decimal CalculateYearCommission(decimal val, bool isFinal, string commsType, int dura)
                        {
                            decimal baseCommission = isFinal
                                ? val * upliftVal
                                : val * upliftVal * supplierCommsVal;

                            if (commsType?.Equals("DURATION", StringComparison.OrdinalIgnoreCase) == true)
                                baseCommission *= duration;

                            return baseCommission;
                        }

                        // Accumulate based on duration and availability
                        switch (duration)
                        {
                            case 1:
                                dueCommission = CalculateYearCommission(year1Data.Value, year1Data.IsFinal, supplierCommsType, duration);
                                break;

                            case 2:
                                dueCommission =
                                    CalculateYearCommission(year1Data.Value, year1Data.IsFinal, supplierCommsType, duration) +
                                    CalculateYearCommission(year2Data.Value, year2Data.IsFinal, supplierCommsType, duration);
                                break;

                            case 3:
                                dueCommission =  //(Year1final * uplift) + (year2final * uplift) + (
                                    CalculateYearCommission(year1Data.Value, year1Data.IsFinal, supplierCommsType, duration) + // (150 * 0.015) + (0 * 0.015) + (0 * 0.015)
                                    CalculateYearCommission(year2Data.Value, year2Data.IsFinal, supplierCommsType, duration) +
                                    CalculateYearCommission(year3Data.Value, year3Data.IsFinal, supplierCommsType, duration);
                                break;

                            case 4:
                                dueCommission =
                                    CalculateYearCommission(year1Data.Value, year1Data.IsFinal, supplierCommsType, duration) +
                                    CalculateYearCommission(year2Data.Value, year2Data.IsFinal, supplierCommsType, duration) +
                                    CalculateYearCommission(year3Data.Value, year3Data.IsFinal, supplierCommsType, duration) +
                                    CalculateYearCommission(year4Data.Value, year4Data.IsFinal, supplierCommsType, duration);
                                break;

                            case 5:
                                dueCommission =
                                    CalculateYearCommission(year1Data.Value, year1Data.IsFinal, supplierCommsType, duration) +
                                    CalculateYearCommission(year2Data.Value, year2Data.IsFinal, supplierCommsType, duration) +
                                    CalculateYearCommission(year3Data.Value, year3Data.IsFinal, supplierCommsType, duration) +
                                    CalculateYearCommission(year4Data.Value, year4Data.IsFinal, supplierCommsType, duration) +
                                    CalculateYearCommission(year5Data.Value, year5Data.IsFinal, supplierCommsType, duration);
                                break;

                            default:
                                dueCommission = CalculateYearCommission(year1Data.Value, year1Data.IsFinal, supplierCommsType, duration);
                                break;
                        }

                        reconciliation.CobanaDueCommission = dueCommission.ToString("F5");
                    }

                    string totalCommissionForecast = "";
                    if (decimal.TryParse(contract.InputEAC, out decimal inputEACVal))
                    {
                        switch (supplierCommsType?.Trim().ToLower())
                        {
                            case "annual":
                                totalCommissionForecast = (inputEACVal * upliftVal).ToString("F5");
                                break;
                            case "residual":
                                totalCommissionForecast = ((inputEACVal * upliftVal) / 12).ToString("F5");
                                break;
                            case "duration":
                                totalCommissionForecast = (inputEACVal * upliftVal * supplierCommsVal).ToString("F5");
                                break;
                            case "quarterly":
                                totalCommissionForecast = ((inputEACVal * upliftVal) / 4).ToString("F5");
                                break;
                        }
                    }

                    string initialCommissionForecast = "";
                    if (decimal.TryParse(totalCommissionForecast, out decimal totalCommissionVal))
                    {
                        initialCommissionForecast = (totalCommissionVal * supplierCommsVal).ToString("F5");
                    }

                    decimal cobanaDue = decimal.TryParse(reconciliation.CobanaDueCommission, out decimal due) ? due : 0;
                    decimal otherAmount = decimal.TryParse(reconciliation.OtherAmount, out decimal other) ? other : 0;

                    var invoiceTotal = eacLogs
                        .Sum(x => decimal.TryParse(x.InvoiceAmount, out decimal inv) ? inv : 0);

                    var finalReconciliation = (cobanaDue + otherAmount - invoiceTotal).ToString("F5");
                    reconciliation.CobanaFinalReconciliation = finalReconciliation;

                    string totalAverageEAC = "";
                    if (!string.IsNullOrWhiteSpace(model.CedCOT))
                    {
                        if (decimal.TryParse(model.CotLostConsumption, out decimal cotLostVal) &&
                            decimal.TryParse(liveDays, out decimal live) && live != 0)
                        {
                            totalAverageEAC = ((cotLostVal / live) * 365).ToString("F2");
                        }
                    }
                    else
                    {
                        // TotalFinalEac i-e Total Average Eac ---- 
                        decimal totalEac = year1Data.Value + year2Data.Value + year3Data.Value + year4Data.Value + year5Data.Value;
                        int duration = int.TryParse(contract?.Duration, out int d) ? d : 1;
                        totalAverageEAC = (totalEac / duration).ToString("F2"); // 0.9151
                    }

                    if (metrics != null)
                    {
                        metrics.ContractDurationDays = contractDurationDays;
                        metrics.LiveDays = liveDays;
                        metrics.PercentLiveDays = percentLiveDays;
                        metrics.TotalCommissionForecast = totalCommissionForecast;
                        metrics.InitialCommissionForecast = initialCommissionForecast;
                        metrics.COTLostReconciliation = cotLostReconciliation;
                        metrics.TotalAverageEAC = totalAverageEAC;
                    }
                    else
                    {
                        _db.CE_CommissionMetrics.Add(new CE_CommissionMetrics
                        {
                            ReconciliationId = reconciliation.Id,
                            ContractDurationDays = contractDurationDays,
                            LiveDays = liveDays,
                            PercentLiveDays = percentLiveDays,
                            TotalCommissionForecast = totalCommissionForecast,
                            InitialCommissionForecast = initialCommissionForecast,
                            COTLostReconciliation = cotLostReconciliation,
                            TotalAverageEAC = totalAverageEAC,
                            contractType = contractType
                        });
                    }

                    #endregion
                }

            }
            catch (Exception ex)
            {
                Logger.Log($"SaveOrUpdateCommissionData failed for EId={model.EId}, Type={contractType}: {ex}");
                throw;
            }
        }


        #endregion



        #region Invoice_Logs

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        [Authorize(Roles = "Accounts,Controls")]
        public async Task<JsonResult> SaveEacLog(CoronaEacLogViewModel model)
        {
            using (var transaction = _db.Database.BeginTransaction())
            {
                try
                {
                    if (!ModelState.IsValid)
                        return Json(JsonResponse.Fail("Please fill all required fields correctly."));

                    if (!(model.SupplierCommsType.Equals("RESIDUAL", StringComparison.OrdinalIgnoreCase) || model.SupplierCommsType.Equals("DURATION", StringComparison.OrdinalIgnoreCase)))
                        return JsonResponse.Fail("Invalid Supplier Comms Type for Corona. Only RESIDUAL or DURATION are allowed.");

                    string durationStr = "1";
                    if (model.ContractType.Equals("Electric", StringComparison.OrdinalIgnoreCase))
                    {
                        durationStr = await _db.CE_ElectricContracts
                            .Where(x => x.EId == model.EId)
                            .Select(x => x.Duration)
                            .FirstOrDefaultAsync();
                    }
                    else if (model.ContractType.Equals("Gas", StringComparison.OrdinalIgnoreCase))
                    {
                        durationStr = await _db.CE_GasContracts
                            .Where(x => x.EId == model.EId)
                            .Select(x => x.Duration)
                            .FirstOrDefaultAsync();
                    }

                    if (!int.TryParse(durationStr, out int duration) || duration <= 0)
                        duration = 1;

                    var log = new CE_EacLogs
                    {
                        EId = model.EId,
                        ContractType = model.ContractType,
                        EacYear = model.EacYear?.Trim(),
                        EacValue = model.EacValue?.Trim(),
                        FinalEac = model.EacValue?.Trim(),
                        InvoiceNo = model.InvoiceNo?.Trim(),
                        InvoiceDate = model.InvoiceDate?.Trim(),
                        PaymentDate = model.PaymentDate?.Trim(),
                        InvoiceAmount = model.InvoiceAmount?.Trim(),
                        CreatedAt = DateTime.Now
                    };

                    _db.CE_EacLogs.Add(log);
                    await _db.SaveChangesAsync();

                    var eacLogs = await _db.CE_EacLogs
                        .Where(l => l.EId == model.EId && l.ContractType == model.ContractType)
                        .OrderByDescending(l => l.CreatedAt)
                        .ToListAsync();

                    decimal averageEac = 0;
                    if (model.SupplierCommsType.Equals("RESIDUAL", StringComparison.OrdinalIgnoreCase))
                    {
                        var TotalEac = GetTotalEacValue(eacLogs, duration);
                        averageEac = TotalEac / duration;
                    }
                    else
                    {
                        var year1 = GetLatestEac(eacLogs, "1ST YEAR EAC-FINAL", "1ST YEAR EAC-INITIAL");
                        var year2 = GetLatestEac(eacLogs, "2ND YEAR EAC-FINAL", "2ND YEAR EAC-INITIAL");
                        var year3 = GetLatestEac(eacLogs, "3RD YEAR EAC-FINAL", "3RD YEAR EAC-INITIAL");
                        var year4 = GetLatestEac(eacLogs, "4TH YEAR EAC-FINAL", "4TH YEAR EAC-INITIAL");
                        var year5 = GetLatestEac(eacLogs, "5TH YEAR EAC-FINAL", "5TH YEAR EAC-INITIAL");

                        var eacValues = new List<decimal> { year1.Value, year2.Value, year3.Value, year4.Value, year5.Value };
                        averageEac = CalculateAverageEac(eacValues, duration);
                    }

                    log.FinalEac = averageEac.ToString("F2");
                    await _db.SaveChangesAsync();

                    var logs = await _db.CE_EacLogs
                        .Where(x => x.EId == model.EId && x.ContractType == model.ContractType)
                        .OrderByDescending(x => x.CreatedAt)
                        .Select(x => new
                        {
                            x.Id,
                            x.EacYear,
                            x.EacValue,
                            x.FinalEac,
                            x.InvoiceNo,
                            x.InvoiceDate,
                            x.PaymentDate,
                            x.InvoiceAmount,
                            x.CreatedAt
                        })
                        .ToListAsync();

                    string mpan = null, mprn = null;
                    if (model.ContractType == "Electric")
                    {
                        mpan = await _db.CE_ElectricContracts
                            .Where(ec => ec.EId == model.EId)
                            .Select(ec => ec.MPAN)
                            .FirstOrDefaultAsync();
                    }
                    else if (model.ContractType == "Gas")
                    {
                        mprn = await _db.CE_GasContracts
                            .Where(gc => gc.EId == model.EId)
                            .Select(gc => gc.MPRN)
                            .FirstOrDefaultAsync();
                    }

                    var formattedLogs = logs.Select(x => new
                    {
                        x.Id,
                        x.EacYear,
                        x.EacValue,
                        x.FinalEac,
                        x.InvoiceNo,
                        InvoiceDate = DateTime.TryParse(x.InvoiceDate, out var dt) ? dt.ToString("dd-MM-yyyy") : x.InvoiceDate,
                        PaymentDate = DateTime.TryParse(x.PaymentDate, out var dtp) ? dtp.ToString("dd-MM-yyyy") : x.PaymentDate,
                        x.InvoiceAmount,
                        MPAN = mpan,
                        MPRN = mprn,
                        Timestamp = x.CreatedAt.ToString("dd-MM-yy HH:mm:ss")
                    }).ToList();

                    transaction.Commit();
                    return JsonResponse.Ok(formattedLogs, "EAC Log saved successfully.");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Logger.Log("SaveEacLog failed: " + ex);
                    return JsonResponse.Fail("Something went wrong while saving the EAC Log.");
                }
            }
        }

        [HttpGet]
        [Authorize(Roles = "Accounts,Controls")]
        public async Task<JsonResult> GetEacLogs(string eid, string contractType)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(eid))
                    return JsonResponse.Fail("Invalid EId.");

                var logs = await _db.CE_EacLogs
                    .Where(x => x.EId == eid && x.ContractType == contractType)
                    .OrderByDescending(x => x.CreatedAt)
                    .ToListAsync();

                string mpan = null, mprn = null;

                if (contractType == "Electric")
                {
                    mpan = await _db.CE_ElectricContracts
                        .Where(c => c.EId == eid)
                        .Select(c => c.MPAN)
                        .FirstOrDefaultAsync();
                }
                else if (contractType == "Gas")
                {
                    mprn = await _db.CE_GasContracts
                        .Where(c => c.EId == eid)
                        .Select(c => c.MPRN)
                        .FirstOrDefaultAsync();
                }

                var formattedLogs = logs.Select(x => new
                {
                    x.Id,
                    x.EacYear,
                    x.EacValue,
                    x.FinalEac,
                    x.InvoiceNo,
                    InvoiceDate = ParserHelper.FormatDateForDisplay(x.InvoiceDate),
                    PaymentDate = ParserHelper.FormatDateForDisplay(x.PaymentDate),
                    x.InvoiceAmount,
                    MPAN = mpan,
                    MPRN = mprn,
                    Timestamp = x.CreatedAt.ToString("dd-MM-yy HH:mm:ss")
                }).ToList();

                return JsonResponse.Ok(formattedLogs);
            }
            catch (Exception ex)
            {
                Logger.Log($"GetEacLogs failed for EId={eid}: " + ex);
                return JsonResponse.Fail("Unable to fetch EAC logs.");
            }
        }

        #endregion

        #region Helper Methods
        private string CalculatePaymentDate(string invoiceDateStr)
        {
            if (DateTime.TryParseExact(invoiceDateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime invoiceDate))
            {
                var fourthWeekStart = invoiceDate.AddDays(21 + 7);

                var diffToFriday = DayOfWeek.Friday - fourthWeekStart.DayOfWeek;
                if (diffToFriday < 0) diffToFriday += 7;
                var paymentDate = fourthWeekStart.AddDays(diffToFriday);

                return paymentDate.ToString("yyyy-MM-dd");
            }
            return null;
        }

        private EacDataViewModel GetLatestEac(List<CE_EacLogs> logs, string finalKey, string initialKey)
        {
            var final = logs.FirstOrDefault(l => l.EacYear == finalKey);
            if (final != null && decimal.TryParse(final.EacValue, out var finalVal))
                return new EacDataViewModel { Value = finalVal, IsFinal = true };

            var initial = logs.FirstOrDefault(l => l.EacYear == initialKey);
            if (initial != null && decimal.TryParse(initial.EacValue, out var initialVal))
                return new EacDataViewModel { Value = initialVal, IsFinal = false };

            return new EacDataViewModel { Value = 0, IsFinal = false };
        }

        private decimal GetTotalEacValue(List<CE_EacLogs> logs, decimal durationInYears)
        {
            if (logs == null || logs.Count == 0 || durationInYears <= 0)
                return 0;

            // Calculate total months based on duration
            var totalMonths = durationInYears * 12;

            decimal totalEac = 0;

            for (int i = 1; i <= totalMonths; i++)
            {
                // Build the exact key e.g. "Awaiting 1st Month Payment"
                string suffix;
                if (i % 10 == 1 && i % 100 != 11) suffix = "st";
                else if (i % 10 == 2 && i % 100 != 12) suffix = "nd";
                else if (i % 10 == 3 && i % 100 != 13) suffix = "rd";
                else suffix = "th";

                var key = $"Awaiting {i}{suffix} Month Payment";

                // Find EAC value for this key
                var eacLog = logs.FirstOrDefault(l => l.EacYear.Equals(key, StringComparison.OrdinalIgnoreCase));

                if (eacLog != null && decimal.TryParse(eacLog.EacValue, out var value))
                {
                    totalEac += value;
                }
            }

            return totalEac;
        }

        private decimal CalculateAverageEac(List<decimal> yearDataList, int duration)
        {
            var total = yearDataList.Sum(data => data);
            return duration > 0 ? total / duration : 0;
        }

        #endregion

    }
}