using CobanaEnergy.Project.Common;
using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Helpers;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.Accounts.InvoiceSupplierDashboard;
using CobanaEnergy.Project.Models.InvoiceSupplierDashboard;
using CsvHelper;
using Logic;
using Logic.ResponseModel.Helper;
using Newtonsoft.Json;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Controllers.Accounts.InvoiceSupplierDashboard
{
    public class InvoiceSupplierDashboardController : BaseController
    {
        private readonly ApplicationDBContext db;

        public InvoiceSupplierDashboardController(ApplicationDBContext _db)
        {
            db = _db;
        }

        #region popup

        [HttpGet]
        [Authorize(Roles = "Accounts,Controls")]
        public async Task<PartialViewResult> InvoiceSupplierPopup()
        {
            try
            {
                var activeSuppliers = await db.CE_Supplier
                    .Where(s => s.Status)
                    .OrderBy(s => s.Name)
                    .ToListAsync();

                var model = new InvoiceSupplierUploadViewModel
                {
                    Suppliers = new SelectList(activeSuppliers, "Id", "Name")
                };

                return PartialView("~/Views/Accounts/InvoiceSupplierDashboard/InvoiceSupplierPopup.cshtml", model);
            }
            catch (Exception ex)
            {
                Logger.Log("InvoiceSupplierPopup: " + ex);
                return PartialView("~/Views/Shared/_ModalError.cshtml", "Failed to load popup.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Accounts,Controls")]
        public async Task<JsonResult> UploadInvoiceFile(InvoiceSupplierUploadViewModel model, HttpPostedFileBase InvoiceFile)
        {
            if (model.SupplierId <= 0 || !await db.CE_Supplier.AnyAsync(s => s.Id == model.SupplierId && s.Status))
                return JsonResponse.Fail("Invalid supplier selected.");

            if (InvoiceFile == null || InvoiceFile.ContentLength == 0)
                return JsonResponse.Fail("Please upload a valid file.");

            if (InvoiceFile.ContentLength > (10 * 1024 * 1024))
                return JsonResponse.Fail("File size exceeds 10 MB limit.");

            var extension = Path.GetExtension(InvoiceFile.FileName)?.ToLowerInvariant();
            if (extension != ".xlsx" && extension != ".xls" && extension != ".csv")
                return JsonResponse.Fail("Only .xlsx, .xls, and .csv files are allowed.");

            try
            {
                var supplier = await db.CE_Supplier.FirstOrDefaultAsync(s => s.Id == model.SupplierId);
                if (supplier == null)
                    return JsonResponse.Fail("Supplier not found.");

                if (string.IsNullOrWhiteSpace(supplier.Name))
                    return JsonResponse.Fail("Invalid supplier. Please contact support.");

                byte[] fileBytes;
                using (var binaryReader = new BinaryReader(InvoiceFile.InputStream))
                {
                    fileBytes = binaryReader.ReadBytes(InvoiceFile.ContentLength);
                }

                List<string> meterNumbers = null;
                using (var memStream = new MemoryStream(fileBytes))
                {
                    if (SupportedSuppliers.Names.Contains(supplier.Name?.Trim()))
                    {
                        meterNumbers = ParseSupplierFile(memStream, extension, supplier.Name);
                    }
                    else
                    {
                        return JsonResponse.Fail("Processing for this supplier is not yet supported. Please contact support.");
                    }

                }

                if (meterNumbers == null || meterNumbers.Count == 0)
                    return JsonResponse.Fail("Could not find MeterNum or MeterPoint column in the uploaded file. Please check your file and try again.");

                var upload = new CE_InvoiceSupplierUploads
                {
                    SupplierId = model.SupplierId,
                    FileName = Path.GetFileName(InvoiceFile.FileName),
                    FileContent = fileBytes,
                    UploadedBy = User.Identity.Name ?? "Unknown",
                    UploadedOn = DateTime.Now
                };

                db.CE_InvoiceSupplierUploads.Add(upload);
                await db.SaveChangesAsync();

                return JsonResponse.Ok(
                       new
                       {
                           redirectUrl = Url.Action("ContractSelectListing", "InvoiceSupplierDashboard", new { uploadId = upload.Id })
                       },
                           "File uploaded and processed successfully.");
            }
            catch (Exception ex)
            {
                Logger.Log("UploadInvoiceFile: " + ex);
                return JsonResponse.Fail("An error occurred while uploading the file.");
            }
        }

        #endregion

        #region [SUPPLIER FILES ]

        private List<string> ParseSupplierFile(Stream fileStream, string extension, string supplierName)
        {
            var uniqueMeterNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int maxScanRows = 120;
            try
            {
                fileStream.Position = 0;
                if (extension == ".xlsx" || extension == ".xls")
                {
                    List<ISheet> sheets = new List<ISheet>();
                    if (extension == ".xlsx")
                    {
                        var workbook = new XSSFWorkbook(fileStream);
                        sheets = GetBackingDataSheet(workbook);
                    }
                    else
                    {
                        var workbook = new HSSFWorkbook(fileStream);
                        sheets = GetBackingDataSheet(workbook);
                    }
                    foreach (var sheet in sheets)
                    {
                        int headerRowIdx = -1;
                        int meterColIdx = -1;

                        for (int rowIdx = sheet.FirstRowNum; rowIdx <= sheet.LastRowNum && rowIdx < maxScanRows; rowIdx++)
                        {
                            var row = sheet.GetRow(rowIdx);
                            if (row == null) continue;

                            for (int cellIdx = 0; cellIdx < row.LastCellNum; cellIdx++)
                            {
                                var cellVal = row.GetCell(cellIdx)?.ToString();
                                if (!string.IsNullOrWhiteSpace(cellVal))
                                {
                                    string normalized = cellVal
                                        .Replace("\u00A0", " ")
                                        .Replace("\u200B", "")
                                        .Replace(" ", "")
                                        .Trim()
                                        .ToLowerInvariant();

                                    //System.Diagnostics.Debug.WriteLine($"Row {rowIdx} Cell {cellIdx}: '{normalized}'");

                                    if (SupportedSuppliers.MeterHeaders.Contains(normalized))
                                    {
                                        headerRowIdx = rowIdx;
                                        meterColIdx = cellIdx;
                                        break;
                                    }
                                }
                            }
                            if (meterColIdx != -1) break;
                        }

                        if (headerRowIdx == -1 || meterColIdx == -1)
                            return uniqueMeterNumbers.ToList();

                        for (int rowIdx = headerRowIdx + 1; rowIdx <= sheet.LastRowNum; rowIdx++)
                        {
                            var row = sheet.GetRow(rowIdx);
                            if (row == null) continue;
                            var val = row.GetCell(meterColIdx)?.ToString()?.Trim();
                            if (!string.IsNullOrWhiteSpace(val))
                                uniqueMeterNumbers.Add(val);
                        }
                    }
                }
                else if (extension == ".csv")
                {
                    fileStream.Position = 0;
                    using (var reader = new StreamReader(fileStream))
                    using (var csv = new CsvReader(reader, new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        IgnoreBlankLines = true,
                        BadDataFound = null,
                        MissingFieldFound = null
                    }))
                    {
                        int meterColIdx = -1;
                        string[] header = null;
                        int rowNum = 0;
                        while (csv.Read())
                        {
                            rowNum++;
                            if (!csv.Context.Parser.Record.All(f => string.IsNullOrWhiteSpace(f)))
                            {
                                header = csv.Context.Parser.Record;
                                for (int i = 0; i < header.Length; i++)
                                {
                                    var h = header[i]
                                        ?.Replace("\u00A0", " ")
                                        .Replace("\u200B", "")
                                        .Replace(" ", "")
                                        .Trim()
                                        .ToLowerInvariant();
                                    if (SupportedSuppliers.MeterHeaders.Contains(h))
                                    {
                                        meterColIdx = i;
                                        break;
                                    }
                                }
                                break;
                            }
                            if (rowNum > maxScanRows) break;
                        }
                        if (meterColIdx == -1)
                            return uniqueMeterNumbers.ToList();

                        while (csv.Read())
                        {
                            var val = csv.GetField(meterColIdx)?.Trim();
                            if (!string.IsNullOrWhiteSpace(val))
                                uniqueMeterNumbers.Add(val);
                        }
                    }
                }
                return uniqueMeterNumbers.ToList();
            }
            catch (Exception ex)
            {
                Logger.Log($"{supplierName}: {ex}");
                return new List<string>();
            }
        }

        #endregion

        #region Helper Methods
        private List<ISheet> GetBackingDataSheet(IWorkbook workbook)
        {

            var requiredSheets = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "backingdata",
                    "invoicedetail",
                    "paymentsdue",
                    "electricity_full_report",
                    "gas_full_report",
                    "80%upfrontcommissionfixed",
                    "80%upfrontcommissionppkwh",
                    "20%reconciliationfixed",
                    "20%reconciliationppkwh",
                    "sheet1",
                    "cobana energy",
                    "self_bill_data"
                };

            var matchingSheets = new List<ISheet>();
            for (int i = 0; i < workbook.NumberOfSheets; i++)
            {
                var name = workbook.GetSheetName(i)?.Replace(" ", "").Trim().ToLowerInvariant();
                if (requiredSheets.Contains(name))
                {
                    matchingSheets.Add(workbook.GetSheetAt(i));
                }
            }
            if (!matchingSheets.Any())
            {
                matchingSheets.Add(workbook.GetSheetAt(0));
            }

            return matchingSheets;

        }

        #endregion

        #region Contract Select Listing

        [HttpGet]

        [Authorize(Roles = "Accounts,Controls")]
        public async Task<ActionResult> ContractSelectListing(int uploadId)
        {
            try
            {
                var upload = await db.CE_InvoiceSupplierUploads.FirstOrDefaultAsync(x => x.Id == uploadId);
                if (upload == null)
                    return JsonResponse.Fail("Could not find uploaded invoice.");

                var supplier = await db.CE_Supplier.FirstOrDefaultAsync(s => s.Id == upload.SupplierId);
                if (supplier == null)
                    return JsonResponse.Fail("Supplier not found for this invoice.");

                var extension = Path.GetExtension(upload.FileName)?.ToLowerInvariant();

                List<string> meterNumbers;
                using (var memStream = new MemoryStream(upload.FileContent))
                {
                    if (SupportedSuppliers.Names.Contains(supplier.Name?.Trim()))
                    {
                        meterNumbers = ParseSupplierFile(memStream, extension, supplier.Name);
                    }
                    else
                    {
                        return JsonResponse.Fail("Processing for this supplier is not yet supported. Please contact support.");
                    }
                }

                var mpans = meterNumbers
                    .Where(x => x.All(char.IsDigit) && x.Length == 13)
                    .ToList();

                var mprns = meterNumbers
                    .Where(x => x.All(char.IsDigit) && x.Length >= 6 && x.Length <= 10)
                    .ToList();

                var excludedKeys = ContractStatusHelper.ExcludedKeys;

                var electricContractsRaw = await db.CE_ElectricContracts
                    .Where(ec => mpans.Contains(ec.MPAN) && ec.SupplierId == upload.SupplierId)
                    .GroupJoin(
                        db.CE_Supplier,
                        ec => ec.SupplierId,
                        s => s.Id,
                        (ec, supplierGroup) => new { ec, Supplier = supplierGroup.FirstOrDefault() }
                    )
                    .GroupJoin(
                        db.CE_ContractStatuses.Where(cs => cs.Type == "Electric"),
                        combined => combined.ec.EId,
                        cs => cs.EId,
                        (combined, statusGroup) => new { combined, Status = statusGroup.FirstOrDefault() }
                    )
                    .GroupJoin(
                            db.CE_CommissionAndReconciliation
                                .Where(cr => cr.contractType == "Electric"),
                            combined => combined.combined.ec.EId,
                            cr => cr.EId,
                            (combined, reconciliationGroup) => new
                            {
                                combined.combined.ec,
                                combined.combined.Supplier,
                                combined.Status,
                                Reconciliation = reconciliationGroup.FirstOrDefault()
                            }
                        )
                    .ToListAsync();

                var filteredElectricContracts = electricContractsRaw
                    .Where(x =>
                        x.Status == null ||
                        !excludedKeys.Contains($"{x.Status.ContractStatus ?? ""}|{x.Status.PaymentStatus ?? ""}")
                    )
                    .Select(x => new
                    {
                        x.ec.EId,
                        SupplierName = x.Supplier?.Name ?? "",
                        x.ec.MPAN,
                        x.ec.InputDate,
                        x.ec.BusinessName,
                        x.ec.InputEAC,
                        x.ec.Duration,
                        ContractStatus = x.Status?.ContractStatus ?? "N/A",
                        PaymentStatus = x.Status?.PaymentStatus ?? "N/A",
                        CED = x.Reconciliation?.CED ?? "N/A"
                    })
                    .ToList();

                var electricContracts = filteredElectricContracts.Select(x => new ContractSelectRowViewModel
                {
                    EId = x.EId,
                    SupplierName = x.SupplierName,
                    MPAN = x.MPAN,
                    MPRN = null,
                    InputDate = DateTime.TryParse(x.InputDate, out var dt) ? dt : (DateTime?)null,
                    BusinessName = x.BusinessName,
                    InputEAC = x.InputEAC,
                    Duration = x.Duration,
                    ContractStatus = x.ContractStatus,
                    PaymentStatus = x.PaymentStatus,
                    CED = ParserHelper.FormatDateForDisplay(x.CED)
                }).ToList();

                var gasContractsRaw = await db.CE_GasContracts
                                    .Where(gc => mprns.Contains(gc.MPRN) && gc.SupplierId == upload.SupplierId)
                                    .GroupJoin(
                                        db.CE_Supplier,
                                        gc => gc.SupplierId,
                                        s => s.Id,
                                        (gc, supplierGroup) => new { gc, Supplier = supplierGroup.FirstOrDefault() }
                                    )
                                    .GroupJoin(
                                        db.CE_ContractStatuses.Where(cs => cs.Type == "Gas"),
                                        combined => combined.gc.EId,
                                        cs => cs.EId,
                                        (combined, statusGroup) => new { combined.gc, combined.Supplier, Status = statusGroup.FirstOrDefault() }
                                    )
                                    .GroupJoin(
                                        db.CE_CommissionAndReconciliation
                                            .Where(cr => cr.contractType == "Gas"),
                                        combined => combined.gc.EId,
                                        cr => cr.EId,
                                        (combined, reconciliationGroup) => new
                                        {
                                            gc = combined.gc,
                                            Supplier = combined.Supplier,
                                            Status = combined.Status,
                                            Reconciliation = reconciliationGroup.FirstOrDefault()
                                        }
                                    )
                                    .ToListAsync();


                var filteredGasContracts = gasContractsRaw
                    .Where(x =>
                        x.Status == null ||
                        !excludedKeys.Contains($"{x.Status.ContractStatus ?? ""}|{x.Status.PaymentStatus ?? ""}")
                    )
                    .Select(x => new
                    {
                        x.gc.EId,
                        SupplierName = x.Supplier?.Name ?? "",
                        x.gc.MPRN,
                        x.gc.InputDate,
                        x.gc.BusinessName,
                        x.gc.InputEAC,
                        x.gc.Duration,
                        ContractStatus = x.Status?.ContractStatus ?? "N/A",
                        PaymentStatus = x.Status?.PaymentStatus ?? "N/A",
                        CED = x.Reconciliation?.CED ?? "N/A"
                    })
                    .ToList();

                var gasContracts = filteredGasContracts.Select(x => new ContractSelectRowViewModel
                {
                    EId = x.EId,
                    SupplierName = x.SupplierName,
                    MPAN = null,
                    MPRN = x.MPRN,
                    InputDate = DateTime.TryParse(x.InputDate, out var dt) ? dt : (DateTime?)null,
                    BusinessName = x.BusinessName,
                    InputEAC = x.InputEAC,
                    Duration = x.Duration,
                    ContractStatus = x.ContractStatus,
                    PaymentStatus = x.PaymentStatus,
                    CED = ParserHelper.FormatDateForDisplay(x.CED)
                }).ToList();

                var allContracts = electricContracts.Concat(gasContracts).ToList();

                var model = new ContractSelectListingViewModel
                {
                    UploadId = uploadId,
                    Contracts = allContracts,
                    SupplierName = allContracts.FirstOrDefault()?.SupplierName ?? "NULL"
                };

                return View("~/Views/Accounts/InvoiceSupplierDashboard/ContractSelectListing.cshtml", model);
            }
            catch (Exception ex)
            {
                Logger.Log("ContractSelectListing: " + ex);
                return View("~/Views/Shared/_ModalError.cshtml", (object)"An error occurred while loading contract listing.");
            }
        }

        #endregion

        #region Confirm Selection & Edit Contracts

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Accounts,Controls")]
        public JsonResult ConfirmSelectionInvoiceSupplier(List<string> selectedContracts)
        {
            try
            {
                if (selectedContracts == null || !selectedContracts.Any())
                    return JsonResponse.Fail("No contracts selected.");

                TempData["SelectedContractIds"] = selectedContracts;
                return JsonResponse.Ok(new { redirectUrl = Url.Action("EditContractsInvoiceSupplier") });
            }
            catch (Exception ex)
            {
                Logger.Log("Error in ConfirmSelectionInvoiceSupplier: " + ex);
                return JsonResponse.Fail("Something went wrong while confirming selection. Please try again later.");
            }
        }

        [HttpGet]
        [Authorize(Roles = "Accounts,Controls")]
        public async Task<ActionResult> EditContractsInvoiceSupplier()
        {
            try
            {
                var selectedIds = TempData["SelectedContractIds"] as List<string>;
                if (selectedIds == null || !selectedIds.Any())
                    return RedirectToAction("NotFound", "Error");

                TempData.Keep("SelectedContractIds");

                var excludedKeys = ContractStatusHelper.ExcludedKeys;

                var electricContractsRaw = await db.CE_ElectricContracts
                    .Where(ec => selectedIds.Contains(ec.EId))
                    .GroupJoin(
                        db.CE_ContractStatuses.Where(cs => cs.Type == "Electric"),
                        ec => ec.EId,
                        cs => cs.EId,
                        (ec, statusGroup) => new { ec, Status = statusGroup.FirstOrDefault() }
                    )
                    .GroupJoin(
                        db.CE_CommissionAndReconciliation
                            .Where(cr => cr.contractType == "Electric"),
                        combined => combined.ec.EId,
                        cr => cr.EId,
                        (combined, reconciliationGroup) => new
                        {
                            combined.ec,
                            combined.Status,
                            Reconciliation = reconciliationGroup.FirstOrDefault()
                        }
                    )
                    .ToListAsync();

                var electricContracts = electricContractsRaw
                    .Where(x =>
                        x.Status == null ||
                        !excludedKeys.Contains($"{x.Status.ContractStatus ?? ""}|{x.Status.PaymentStatus ?? ""}")
                    )
                    .Select(x => new
                    {
                        x.ec,
                        x.Status,
                        CED = x.Reconciliation?.CED ?? "N/A",
                        CED_COT = x.Reconciliation?.CED_COT ?? "N/A"
                    })
                    .ToList();

                var gasContractsRaw = await db.CE_GasContracts
                    .Where(gc => selectedIds.Contains(gc.EId))
                    .GroupJoin(
                        db.CE_ContractStatuses.Where(cs => cs.Type == "Gas"),
                        gc => gc.EId,
                        cs => cs.EId,
                        (gc, statusGroup) => new { gc, Status = statusGroup.FirstOrDefault() }
                    )
                    .GroupJoin(
                        db.CE_CommissionAndReconciliation
                            .Where(cr => cr.contractType == "Gas"),
                        combined => combined.gc.EId,
                        cr => cr.EId,
                        (combined, reconciliationGroup) => new
                        {
                            combined.gc,
                            combined.Status,
                            Reconciliation = reconciliationGroup.FirstOrDefault()
                        }
                    )
                    .ToListAsync();

                var gasContracts = gasContractsRaw
                    .Where(x =>
                        x.Status == null ||
                        !excludedKeys.Contains($"{x.Status.ContractStatus ?? ""}|{x.Status.PaymentStatus ?? ""}")
                    )
                    .Select(x => new
                    {
                        x.gc,
                        x.Status,
                        CED = x.Reconciliation?.CED ?? "N/A",
                        CED_COT = x.Reconciliation?.CED_COT ?? "N/A"
                    })
                    .ToList();

                var electricSnapshots = await db.CE_ElectricSupplierSnapshots
                    .Where(x => selectedIds.Contains(x.EId))
                    .ToListAsync();
                var electricSnapshotDict = electricSnapshots.ToDictionary(x => x.EId, x => x);
                var electricSnapshotIds = electricSnapshots.Select(x => x.Id).ToList();

                var gasSnapshots = await db.CE_GasSupplierSnapshots
                    .Where(x => selectedIds.Contains(x.EId))
                    .ToListAsync();
                var gasSnapshotDict = gasSnapshots.ToDictionary(x => x.EId, x => x);
                var gasSnapshotIds = gasSnapshots.Select(x => x.Id).ToList();

                var electricProductSnapshotDict = await db.CE_ElectricSupplierProductSnapshots
                    .Where(x => electricSnapshotIds.Contains(x.SnapshotId))
                    .GroupBy(x => x.SnapshotId)
                    .ToDictionaryAsync(g => g.Key, g => g.FirstOrDefault());

                var electricUpliftSnapshotDict = await db.CE_ElectricSupplierUpliftSnapshots
                    .Where(x => electricSnapshotIds.Contains(x.SnapshotId))
                    .GroupBy(x => x.SnapshotId)
                    .ToDictionaryAsync(g => g.Key, g => g.FirstOrDefault());

                var gasProductSnapshotDict = await db.CE_GasSupplierProductSnapshots
                    .Where(x => gasSnapshotIds.Contains(x.SnapshotId))
                    .GroupBy(x => x.SnapshotId)
                    .ToDictionaryAsync(g => g.Key, g => g.FirstOrDefault());

                var gasUpliftSnapshotDict = await db.CE_GasSupplierUpliftSnapshots
                    .Where(x => gasSnapshotIds.Contains(x.SnapshotId))
                    .GroupBy(x => x.SnapshotId)
                    .ToDictionaryAsync(g => g.Key, g => g.FirstOrDefault());

                var contracts = new List<ContractEditRowViewModel>();

                foreach (var x in electricContracts)
                {
                    string uplift = "N/A";
                    string supplierCommsType = "N/A";
                    string supplierName = "N/A";

                    if (electricSnapshotDict.TryGetValue(x.ec.EId, out var snapshot))
                    {
                        var snapshotId = snapshot.Id;
                        supplierName = snapshot.SupplierName ?? "N/A";

                        if (electricUpliftSnapshotDict.TryGetValue(snapshotId, out var upliftSnap) && upliftSnap != null)
                            uplift = upliftSnap.Uplift ?? "N/A";

                        if (electricProductSnapshotDict.TryGetValue(snapshotId, out var productSnap) && productSnap != null)
                            supplierCommsType = productSnap.SupplierCommsType ?? "N/A";
                    }

                    contracts.Add(new ContractEditRowViewModel
                    {
                        EId = x.ec.EId,
                        MPAN = x.ec.MPAN,
                        MPRN = null,
                        InputDate = ParserHelper.FormatDateForDisplay(x.ec.InputDate),
                        BusinessName = x.ec.BusinessName,
                        StartDate = ParserHelper.FormatDateForDisplay(x.ec.InitialStartDate),
                        Duration = x.ec.Duration,
                        Uplift = uplift,
                        SupplierCommsType = supplierCommsType,
                        CED = ParserHelper.FormatDateForDisplay(x.CED),
                        CED_COT = ParserHelper.FormatDateForDisplay(x.CED_COT),
                        FuelType = "Electric",
                        SupplierName = supplierName,
                        SupplierId = snapshot?.SupplierId ?? 0
                    });
                }

                foreach (var x in gasContracts)
                {
                    string uplift = "N/A";
                    string supplierCommsType = "N/A";
                    string supplierName = "N/A";

                    if (gasSnapshotDict.TryGetValue(x.gc.EId, out var snapshot))
                    {
                        var snapshotId = snapshot.Id;
                        supplierName = snapshot.SupplierName ?? "N/A";

                        if (gasUpliftSnapshotDict.TryGetValue(snapshotId, out var upliftSnap) && upliftSnap != null)
                            uplift = upliftSnap.Uplift ?? "N/A";

                        if (gasProductSnapshotDict.TryGetValue(snapshotId, out var productSnap) && productSnap != null)
                            supplierCommsType = productSnap.SupplierCommsType ?? "N/A";
                    }

                    contracts.Add(new ContractEditRowViewModel
                    {
                        EId = x.gc.EId,
                        MPAN = null,
                        MPRN = x.gc.MPRN,
                        InputDate = ParserHelper.FormatDateForDisplay(x.gc.InputDate),
                        BusinessName = x.gc.BusinessName,
                        StartDate = ParserHelper.FormatDateForDisplay(x.gc.InitialStartDate),
                        Duration = x.gc.Duration,
                        Uplift = uplift,
                        SupplierCommsType = supplierCommsType,
                        CED = ParserHelper.FormatDateTimeForDisplay(x.CED),
                        CED_COT = ParserHelper.FormatDateTimeForDisplay(x.CED_COT),
                        FuelType = "Gas",
                        SupplierName = supplierName,
                        SupplierId = snapshot?.SupplierId ?? 0
                    });
                }

                var model = new ContractEditTableViewModel
                {
                    Contracts = contracts
                };

                return View("~/Views/Accounts/InvoiceSupplierDashboard/EditContractsInvoiceSupplier.cshtml", model);
            }
            catch (Exception ex)
            {
                Logger.Log("Error in EditContractsInvoiceSupplier" + ex);
                return RedirectToAction("NotFound", "Error");
            }
        }


        #endregion

    }
}