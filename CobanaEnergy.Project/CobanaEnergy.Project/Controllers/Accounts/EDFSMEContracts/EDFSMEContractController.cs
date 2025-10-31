using CobanaEnergy.Project.Common;
using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Helpers;
using CobanaEnergy.Project.Filters;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.Accounts;
using CobanaEnergy.Project.Models.Accounts.SuppliersModels;
using CobanaEnergy.Project.Models.Accounts.SuppliersModels.BGB;
using CobanaEnergy.Project.Models.Accounts.SuppliersModels.BGB.DBModel;
using CobanaEnergy.Project.Models.Accounts.SuppliersModels.EDFSME;
using CobanaEnergy.Project.Models.Supplier.SupplierDBModels.snapshot;
using Logic;
using Logic.ResponseModel.Helper;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Controllers.Accounts.EDFSMEContracts
{
    public class EDFSMEContractController : BaseController
    {
        private readonly ApplicationDBContext _db;
        public EDFSMEContractController(ApplicationDBContext db)
        {
            _db = db;
        }

        #region EDFSMEContract 

        [HttpGet]
        [Authorize(Roles = "Accounts,Controls")]
        public async Task<ActionResult> EditEDFSMEContract(string id, string supplierId, string type)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id) || !Guid.TryParse(id, out _) ||
                    string.IsNullOrWhiteSpace(supplierId) || !int.TryParse(supplierId, out _) ||
                    string.IsNullOrWhiteSpace(type))
                {
                    return HttpNotFound("Invalid ID, SupplierId, or Type.");
                }

                var model = new EditEDFSmeContractViewModel
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

                return View("~/Views/Accounts/EDFSMEContract/EditEDFSMEContract.cshtml", model);
            }
            catch (Exception ex)
            {
                Logger.Log($"EditEDFSMEContract failed for id={id}, supplierId={supplierId}, type={type}: {ex}");
                return RedirectToAction("NotFound", "Error");
            }
        }
        private async Task ReconciliationAndCommsssionMetrics(string id, EditEDFSmeContractViewModel model, string contractType)
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
                            if (!(model.SupplierCommsTypeElectric.Equals("DURATION", StringComparison.OrdinalIgnoreCase)))
                                return JsonResponse.Fail("Invalid Supplier Comms Type for Crown. Only DURATION are allowed.");


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
                            if (!(model.SupplierCommsTypeGas.Equals("DURATION", StringComparison.OrdinalIgnoreCase)))
                                return JsonResponse.Fail("Invalid Supplier Comms Type for Crown. Only DURATION are allowed.");

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
                Dashboard = "EDF SME Contracts",
                Username = User?.Identity?.Name ?? "Unknown User"
            };
            PaymentLogsHelper.InsertPaymentAndNotesLogs(_db, notesModel);
        }

        private async Task SaveOrUpdateCommissionData(UpdateContractViewModel model, string contractType)
        {
            try
            {
                dynamic contract = null;

                if (contractType == "Electric")
                {
                    contract = await _db.CE_ElectricContracts
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.EId == model.EId);
                }
                else if (contractType == "Gas")
                {
                    contract = await _db.CE_GasContracts
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.EId == model.EId);
                }

                if (contract == null)
                {
                    Logger.Log($"No contract found for EId={model.EId}, Type={contractType}");
                    return;
                }

                string uplift = (contractType == "Electric") ? model.UpliftElectric : model.UpliftGas;
                string supplierCommsType = (contractType == "Electric") ? model.SupplierCommsTypeElectric : model.SupplierCommsTypeGas;
                string commission = (contractType == "Electric") ? model.CommissionElectric : model.CommissionGas;

                decimal.TryParse(uplift, out decimal upliftVal);
                decimal.TryParse(commission, out decimal supplierCommsVal);

                var eacLogs = await _db.CE_EacLogs
                    .Where(l => l.EId == model.EId && l.ContractType == contractType)
                    .OrderByDescending(l => l.CreatedAt)
                    .ToListAsync();

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
                    int duration = int.TryParse(contract?.Duration, out int d) ? d : 1;

                    liveDays = (cedCOTDate - startDt).TotalDays.ToString("F5");

                    if (decimal.TryParse(liveDays, out decimal live))
                    {
                        cotLostReconciliation = CalculateAverageInvoicePerDay(eacLogs, live).ToString("F5");
                    }

                    foreach (var log in eacLogs)
                    {
                        log.FinalEac = cotLostReconciliation;
                        supplierEacFinal = log.FinalEac;
                    }

                    decimal CalculateYearCommission(decimal val, bool isFinal, string commsType, int dura)
                    {
                        decimal baseCommission = isFinal
                            ? val * upliftVal
                            : val * upliftVal * supplierCommsVal;

                        if (commsType?.Equals("DURATION", StringComparison.OrdinalIgnoreCase) == true)
                            baseCommission *= duration;

                        return baseCommission;
                    }

                    var eacValue = GetLatestEac(eacLogs);
                    decimal dueCommission = CalculateYearCommission(eacValue.Value, eacValue.IsFinal, supplierCommsType, duration);
                    reconciliation.CobanaDueCommission = dueCommission.ToString("F5");

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

                    var eacValue = GetLatestEac(eacLogs);
                    dueCommission = CalculateYearCommission(eacValue.Value, eacValue.IsFinal, supplierCommsType, duration);

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


                if (metrics != null)
                {
                    metrics.ContractDurationDays = contractDurationDays;
                    metrics.LiveDays = liveDays;
                    metrics.PercentLiveDays = percentLiveDays;
                    metrics.TotalCommissionForecast = totalCommissionForecast;
                    metrics.InitialCommissionForecast = initialCommissionForecast;
                    metrics.COTLostReconciliation = cotLostReconciliation;
                    metrics.TotalAverageEAC = "0.00";
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
                        TotalAverageEAC = "0.00",
                        contractType = contractType
                    });
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
        public async Task<JsonResult> SaveEacLog(EDFSmeEacLogViewModel model)
        {
            using (var transaction = _db.Database.BeginTransaction())
            {
                try
                {
                    if (!ModelState.IsValid)
                        return Json(JsonResponse.Fail("Please fill all required fields correctly."));

                    if (!(model.SupplierCommsType.Equals("DURATION", StringComparison.OrdinalIgnoreCase)))
                        return JsonResponse.Fail("Invalid Supplier Comms Type for EDF SME. Only DURATION are allowed.");


                    string durationStr = "1";
                    if (model.ContractType.Equals("Electric", StringComparison.OrdinalIgnoreCase))
                    {
                        durationStr = await _db.CE_ElectricContracts
                            .Where(x => x.EId == model.EId)
                            .Select(x => x.Duration)
                            .FirstOrDefaultAsync();


                        #region [Uplift]

                        var snapshot = _db.CE_ElectricSupplierSnapshots
                             .Include(s => s.CE_ElectricSupplierUpliftSnapshots)
                             .FirstOrDefault(s => s.EId == model.EId);

                        if (snapshot == null)
                            return JsonResponse.Fail("Snapshot not found.");

                        var maxUplift = snapshot.CE_ElectricSupplierUpliftSnapshots
                                                .Where(u => u.FuelType == "Electric")
                                                .OrderByDescending(u => u.EndDate)
                                                .FirstOrDefault();

                        if (maxUplift == null)
                            return JsonResponse.Ok(null);

                        model.UpliftGas = maxUplift.Uplift;

                        #endregion



                    }
                    else if (model.ContractType.Equals("Gas", StringComparison.OrdinalIgnoreCase))
                    {
                        durationStr = await _db.CE_GasContracts
                            .Where(x => x.EId == model.EId)
                            .Select(x => x.Duration)
                            .FirstOrDefaultAsync();

                        #region [Uplift]


                        var snapshot = _db.CE_GasSupplierSnapshots
                            .Include(s => s.CE_GasSupplierUpliftSnapshots)
                            .FirstOrDefault(s => s.EId == model.EId);
                        if (snapshot == null)
                            return JsonResponse.Fail("Snapshot not found.");

                        var maxUplift = snapshot.CE_GasSupplierUpliftSnapshots
                                                .Where(u => u.FuelType == "Gas")
                                                .OrderByDescending(u => u.EndDate)
                                                .FirstOrDefault();

                        if (maxUplift == null)
                            return JsonResponse.Ok(null);

                        model.UpliftGas = maxUplift.Uplift;

                        #endregion


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
                    // 

                    var edfEac = CalculateAverageEac(model.EacValue, duration, model.Commission, model.Uplift, model.EacYear);
                    log.SupplierEac = edfEac.ToString("F2");
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
                            x.SupplierEac,
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
                        x.SupplierEac,
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
                    x.SupplierEac,
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

        private EacDataViewModel GetLatestEac(List<CE_EacLogs> logs)
        {
            if (logs == null || logs.Count == 0)
                return new EacDataViewModel { Value = 0, IsFinal = false };
            var validLogs = logs
                .Where(l => !string.IsNullOrWhiteSpace(l.EacValue))
                .ToList();

            if (!validLogs.Any())
                return new EacDataViewModel { Value = 0, IsFinal = false };

            var priorityOrder = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                 {
                     { "Final", 1 },
                     { "D19", 2 },
                     { "Initial", 3 }
                 };

            var latestLog = validLogs
                .Where(l => priorityOrder.ContainsKey(l.EacYear))
                .OrderBy(l => priorityOrder[l.EacYear])
                .ThenByDescending(l => l.CreatedAt)
                .FirstOrDefault();

            if (latestLog == null || !decimal.TryParse(latestLog.EacValue, out var value))
                return new EacDataViewModel { Value = 0, IsFinal = false };

            return new EacDataViewModel
            {
                Value = value,
                IsFinal = latestLog.EacYear.Equals("Final", StringComparison.OrdinalIgnoreCase)
            };
        }

        private decimal CalculateAverageEac(string initialVal, int duration, string supplierComms, string uplift, string eacYear)
        {
            decimal initial = ParseDecimalOrThrow(initialVal, nameof(initialVal));
            decimal supplier = ParseDecimalOrThrow(supplierComms, nameof(supplierComms));
            decimal upliftVal = ParseDecimalOrThrow(uplift, nameof(uplift));
            decimal result = 0m;
            if (eacYear == "Final")
                result = initial * upliftVal * duration;
            else result = initial * upliftVal * duration * supplier;

            return Math.Truncate(result);
        }

        private decimal CalculateAverageInvoicePerDay(List<CE_EacLogs> logs, decimal liveDays)
        {
            if (logs == null || logs.Count == 0 || liveDays <= 0)
                return 0;

            var filteredLogs = logs
                .Where(l => l.EacYear.Equals("Final", StringComparison.OrdinalIgnoreCase)
                         || l.EacYear.Equals("D19", StringComparison.OrdinalIgnoreCase)
                         || l.EacYear.Equals("Initial", StringComparison.OrdinalIgnoreCase))
                .ToList();

            decimal totalInvoice = filteredLogs
                .Where(l => decimal.TryParse(l.InvoiceAmount, out _)) // Ensure valid decimal
                .Sum(l => Convert.ToDecimal(l.InvoiceAmount));

            return totalInvoice / liveDays;
        }

        private decimal ParseDecimalOrThrow(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{paramName} cannot be null or empty.", paramName);

            if (!decimal.TryParse(value, out decimal parsed))
                throw new FormatException($"Invalid decimal format for {paramName}: '{value}'");

            return parsed;
        }


        #endregion

    }
}