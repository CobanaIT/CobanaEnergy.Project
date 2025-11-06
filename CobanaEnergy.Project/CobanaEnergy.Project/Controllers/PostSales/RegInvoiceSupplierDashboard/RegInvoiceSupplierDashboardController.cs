using CobanaEnergy.Project.Common;
using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Filters;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.Accounts.InvoiceSupplierDashboard;
using CobanaEnergy.Project.Models.Accounts.SuppliersModels.BGB.DBModel;
using CobanaEnergy.Project.Models.PostSales.Entities;
using CobanaEnergy.Project.Models.PostSales.RegInvoiceSupplierDashboard;
using CobanaEnergy.Project.Models.PostSales.RegInvoiceSupplierDashboard.DB_Model;
using CobanaEnergy.Project.Models.PostSales.RegInvoiceSupplierDashboard.Dto;
using CsvHelper;
using Logic;
using Logic.ResponseModel.Helper;
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

namespace CobanaEnergy.Project.Controllers.PostSales.RegInvoiceSupplierDashboard
{
    [Authorize(Roles = "Post-sales")]
    public class RegInvoiceSupplierDashboardController : BaseController
    {
        private readonly ApplicationDBContext db;

        public RegInvoiceSupplierDashboardController(ApplicationDBContext _db)
        {
            db = _db;
        }


        #region popup

        [HttpGet]
        public async Task<PartialViewResult> RegInvoiceSupplierPopup()
        {
            try
            {
                var activeSuppliers = await db.CE_Supplier
                  .Where(s => s.Status)
                     .OrderBy(s => s.Name)
                              .ToListAsync();

                var model = new RegInvoiceSupplierUploadViewModel
                {
                    Suppliers = new SelectList(activeSuppliers, "Id", "Name")
                };

                return PartialView("~/Views/PostSales/RegInvoiceSupplierDashboard/RegInvoiceSupplierPopup.cshtml", model);
            }
            catch (Exception ex)
            {
                Logger.Log("RegInvoiceSupplierPopup: " + ex);
                return PartialView("~/Views/Shared/_ModalError.cshtml", "Failed to load popup.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> UploadInvoiceFile(RegInvoiceSupplierUploadViewModel model, HttpPostedFileBase InvoiceFile)
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

                var regFiles = new List<RegFileUploadDto>();
                using (var memStream = new MemoryStream(fileBytes))
                {
                    if (SupportedSuppliers.Names.Contains(supplier.Name?.Trim()))
                    {
                        regFiles = ParseSupplierFile(memStream, extension, supplier.Name);
                    }
                    else
                    {
                        return JsonResponse.Fail("Processing for this supplier is not yet supported. Please contact support.");
                    }

                }

                if (regFiles == null || regFiles.Count == 0)
                    return JsonResponse.Fail("Could not find MeterNum or MeterPoint column in the uploaded file. Please check your file and try again.");

                var upload = new CE_RegSupplierFileUploads
                {
                    SupplierId = model.SupplierId,
                    FileName = Path.GetFileName(InvoiceFile.FileName),
                    FileContent = fileBytes,
                    UploadedBy = User.Identity.Name ?? "Unknown",
                    UploadedOn = DateTime.Now
                };

                db.CE_RegSupplierFileUploads.Add(upload);
                await db.SaveChangesAsync();

                return JsonResponse.Ok(
                        new
                        {
                            redirectUrl = Url.Action("ContractSelectListing", "RegInvoiceSupplierDashboard", new { uploadId = upload.Id })
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

        private List<RegFileUploadDto> ParseSupplierFile(Stream fileStream, string extension, string supplierName)
        {
            var regFiles = new List<RegFileUploadDto>();
            try
            {
                fileStream.Position = 0;
                if (extension == ".xlsx" || extension == ".xls")
                {
                    ISheet sheet = null;
                    if (extension == ".xlsx")
                    {
                        var workbook = new XSSFWorkbook(fileStream);
                        sheet = GetRequiredColumnsFromSheet(workbook);
                    }
                    else
                    {
                        var workbook = new HSSFWorkbook(fileStream);
                        sheet = GetRequiredColumnsFromSheet(workbook);
                    }

                    if (sheet != null)
                    {
                        regFiles = ParseExcelToDto(sheet, supplierName);
                    }
                }
                else if (extension == ".csv")
                {
                    fileStream.Position = 0;
                    regFiles = ParseCsvToDto(fileStream, supplierName);
                }

                return regFiles;
            }
            catch (Exception ex)
            {
                Logger.Log($"{supplierName}: {ex}");
                return new List<RegFileUploadDto>();
            }
        }

        #endregion

        #region Helper Methods
        private ISheet GetRequiredColumnsFromSheet(IWorkbook workbook)
        {
            int maxScanRows = 20;

            // Since only one sheet is available, get the first sheet
            var sheet = workbook.GetSheetAt(0);
            if (sheet == null) return null;

            // Look for header row in first 20 rows
            for (int rowIdx = sheet.FirstRowNum; rowIdx <= sheet.LastRowNum && rowIdx < maxScanRows; rowIdx++)
            {
                var row = sheet.GetRow(rowIdx);
                if (row == null) continue;

                bool hasBusinessName = false;
                bool hasMPXN = false;
                bool hasInputDate = false;
                bool hasSupplierName = false;
                bool hasPostCode = false;


                // Scan all cells in the row for required columns
                for (int cellIdx = 0; cellIdx < row.LastCellNum; cellIdx++)
                {
                    var cellVal = row.GetCell(cellIdx)?.ToString();
                    if (!string.IsNullOrWhiteSpace(cellVal))
                    {
                        // Check against all possible column variations
                        if (SupplierColumnMappings.IsBusinessNameColumn(cellVal))
                            hasBusinessName = true;
                        else if (SupplierColumnMappings.IsMpxnColumn(cellVal))
                            hasMPXN = true;
                        else if (SupplierColumnMappings.IsInputDateColumn(cellVal))
                            hasInputDate = true;
                        else if (SupplierColumnMappings.IsSupplierNameColumn(cellVal))
                            hasSupplierName = true;
                        else if (SupplierColumnMappings.IsPostCodeColumn(cellVal))
                            hasPostCode = true;
                    }
                }

                // If any required columns are found, return this sheet
                if (hasBusinessName || hasMPXN || hasInputDate ||
                    hasSupplierName || hasPostCode)
                {
                    return sheet;
                }
            }

            return null;
        }

        private List<RegFileUploadDto> ParseExcelToDto(ISheet sheet, string supplierName)
        {
            var results = new List<RegFileUploadDto>();
            int maxScanRows = 20;
            int headerRowIdx = -1;

            // Column indices
            int businessNameIdx = -1;
            int mpxnIdx = -1;
            int inputDateIdx = -1;
            int supplierNameIdx = -1;
            int postCodeIdx = -1;

            // Find header row and column indices
            for (int rowIdx = sheet.FirstRowNum; rowIdx <= sheet.LastRowNum && rowIdx < maxScanRows; rowIdx++)
            {
                var row = sheet.GetRow(rowIdx);
                if (row == null) continue;

                for (int cellIdx = 0; cellIdx < row.LastCellNum; cellIdx++)
                {
                    var cellVal = row.GetCell(cellIdx)?.ToString();
                    if (!string.IsNullOrWhiteSpace(cellVal))
                    {
                        // Check against all possible column variations
                        if (SupplierColumnMappings.IsBusinessNameColumn(cellVal))
                            businessNameIdx = cellIdx;
                        else if (SupplierColumnMappings.IsMpxnColumn(cellVal))
                            mpxnIdx = cellIdx;
                        else if (SupplierColumnMappings.IsInputDateColumn(cellVal))
                            inputDateIdx = cellIdx;
                        else if (SupplierColumnMappings.IsSupplierNameColumn(cellVal))
                            supplierNameIdx = cellIdx;
                        else if (SupplierColumnMappings.IsPostCodeColumn(cellVal))
                            postCodeIdx = cellIdx;
                    }
                }

                // Check if we found the required columns
                if (businessNameIdx != -1 || mpxnIdx != -1 || inputDateIdx != -1 || supplierNameIdx != -1 || postCodeIdx != -1)
                {
                    headerRowIdx = rowIdx;
                    break;
                }
            }

            if (headerRowIdx == -1)
                return results;

            // Parse data rows
            for (int rowIdx = headerRowIdx + 1; rowIdx <= sheet.LastRowNum; rowIdx++)
            {
                var row = sheet.GetRow(rowIdx);
                if (row == null) continue;

                var mpxnValue = row.GetCell(mpxnIdx)?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(mpxnValue))
                    continue;
                var dto = new RegFileUploadDto
                {
                    BusinessName = row.GetCell(businessNameIdx)?.ToString()?.Trim(),
                    InputDate = row.GetCell(inputDateIdx)?.ToString()?.Trim(),
                    SupplierName = supplierNameIdx != -1 ? row.GetCell(supplierNameIdx)?.ToString()?.Trim() : null,
                    PostCode = postCodeIdx != -1 ? row.GetCell(postCodeIdx)?.ToString()?.Trim() : null
                };

                // Determine if MPXN is MPAN or MPRN based on length
                int mpxnLength = mpxnValue.Length;
                if (mpxnLength >= 6 && mpxnLength <= 10)
                {
                    dto.MPRN = mpxnValue;
                    dto.MPAN = null;
                }
                else if (mpxnLength == 13)
                {
                    dto.MPAN = mpxnValue;
                    dto.MPRN = null;
                }
                else
                {
                    // If length doesn't match expected, skip or log
                    continue;
                }

                results.Add(dto);
            }

            return results;
        }

        private List<RegFileUploadDto> ParseCsvToDto(Stream fileStream, string supplierName)
        {
            var results = new List<RegFileUploadDto>();
            int maxScanRows = 20;
            int headerRowIdx = -1;

            // Column indices
            int businessNameIdx = -1;
            int mpxnIdx = -1;
            int inputDateIdx = -1;
            int supplierNameIdx = -1;
            int postCodeIdx = -1;

            try
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
                    // Find header row and column indices
                    int rowNum = 0;
                    while (csv.Read() && rowNum < maxScanRows)
                    {
                        rowNum++;

                        // Check if this row has non-empty fields
                        if (csv.Context.Parser.Record.All(f => string.IsNullOrWhiteSpace(f)))
                            continue;

                        // Check if this is the header row
                        bool hasAnyColumn = false;
                        for (int i = 0; i < csv.Context.Parser.Record.Length; i++)
                        {
                            var cellVal = csv.Context.Parser.Record[i];
                            if (!string.IsNullOrWhiteSpace(cellVal))
                            {
                                // Check against all possible column variations
                                if (SupplierColumnMappings.IsBusinessNameColumn(cellVal))
                                {
                                    businessNameIdx = i;
                                    hasAnyColumn = true;
                                }
                                else if (SupplierColumnMappings.IsMpxnColumn(cellVal))
                                {
                                    mpxnIdx = i;
                                    hasAnyColumn = true;
                                }
                                else if (SupplierColumnMappings.IsInputDateColumn(cellVal))
                                {
                                    inputDateIdx = i;
                                    hasAnyColumn = true;
                                }
                                else if (SupplierColumnMappings.IsSupplierNameColumn(cellVal))
                                {
                                    supplierNameIdx = i;
                                    hasAnyColumn = true;
                                }
                                else if (SupplierColumnMappings.IsPostCodeColumn(cellVal))
                                {
                                    postCodeIdx = i;
                                    hasAnyColumn = true;
                                }
                            }
                        }

                        // If we found any recognized columns, this is our header row
                        if (hasAnyColumn && businessNameIdx != -1 || mpxnIdx != -1
                            || inputDateIdx != -1 || supplierNameIdx != -1 || postCodeIdx != -1)
                        {
                            headerRowIdx = rowNum;
                            break;
                        }
                    }

                    // If header row not found, return empty results
                    if (headerRowIdx == -1)
                        return results;

                    // Parse data rows
                    while (csv.Read())
                    {
                        var record = csv.Context.Parser.Record;

                        // Skip empty rows
                        if (record.All(f => string.IsNullOrWhiteSpace(f)))
                            continue;

                        // Get MPXN value
                        var mpxnValue = mpxnIdx < record.Length ? record[mpxnIdx]?.Trim() : null;
                        if (string.IsNullOrWhiteSpace(mpxnValue))
                            continue;

                        var dto = new RegFileUploadDto
                        {
                            BusinessName = businessNameIdx != -1 && businessNameIdx < record.Length
                             ? record[businessNameIdx]?.Trim()
                             : null,
                            InputDate = inputDateIdx != -1 && inputDateIdx < record.Length
                                     ? record[inputDateIdx]?.Trim()
                                    : null,
                            SupplierName = supplierNameIdx != -1 && supplierNameIdx < record.Length
                            ? record[supplierNameIdx]?.Trim()
                            : null,
                            PostCode = postCodeIdx != -1 && postCodeIdx < record.Length
                            ? record[postCodeIdx]?.Trim()
                            : null
                        };

                        // Determine if MPXN is MPAN or MPRN based on length
                        int mpxnLength = mpxnValue.Length;
                        if (mpxnLength >= 6 && mpxnLength <= 10)
                        {
                            dto.MPRN = mpxnValue;
                            dto.MPAN = null;
                        }
                        else if (mpxnLength == 13)
                        {
                            dto.MPAN = mpxnValue;
                            dto.MPRN = null;
                        }
                        else
                        {
                            // If length doesn't match expected, skip or log
                            continue;
                        }

                        results.Add(dto);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"ParseCsvToDto - {supplierName}: {ex}");
            }

            return results;
        }

        #endregion

        #region Contract Select Listing

        [HttpGet]

        [Authorize(Roles = "Accounts,Controls")]
        public async Task<ActionResult> ContractSelectListing(int uploadId)
        {
            try
            {
                var upload = await db.CE_RegSupplierFileUploads.FirstOrDefaultAsync(x => x.Id == uploadId);
                if (upload == null)
                    return JsonResponse.Fail("Could not find uploaded invoice.");

                var supplier = await db.CE_Supplier.FirstOrDefaultAsync(s => s.Id == upload.SupplierId);
                if (supplier == null)
                    return JsonResponse.Fail("Supplier not found for this invoice.");

                var extension = Path.GetExtension(upload.FileName)?.ToLowerInvariant();

                var regFiles = new List<RegFileUploadDto>();
                using (var memStream = new MemoryStream(upload.FileContent))
                {
                    if (SupportedSuppliers.Names.Contains(supplier.Name?.Trim()))
                    {
                        regFiles = ParseSupplierFile(memStream, extension, supplier.Name);
                    }
                    else
                    {
                        return JsonResponse.Fail("Processing for this supplier is not yet supported. Please contact support.");
                    }
                }

                // Filter regFiles with valid MPAN/MPRN
                var mpans = regFiles
                    .Where(x => !string.IsNullOrWhiteSpace(x.MPAN) && x.MPAN.All(char.IsDigit) && x.MPAN.Length == 13)
                    .ToList();

                var mprns = regFiles
                    .Where(x => !string.IsNullOrWhiteSpace(x.MPRN) && x.MPRN.All(char.IsDigit) && x.MPRN.Length >= 6 && x.MPRN.Length <= 10)
                    .ToList();

                var electricContracts = new List<RegContractSelectRowViewModel>();
                // Apply MPAN filter if any MPANs exist
                if (mpans.Any())
                {
                    // Build dynamic query for Electric Contracts
                    var electricQuery = db.CE_ElectricContracts.AsQueryable();
                    var mpanList = mpans.Select(x => x.MPAN).ToList();
                    electricQuery = electricQuery.Where(ec => mpanList.Contains(ec.MPAN));


                    // Apply Supplier filter
                    electricQuery = electricQuery.Where(ec => ec.SupplierId == upload.SupplierId);

                    // Execute query first, then filter in memory
                    var electricContractsRaw = await electricQuery
                        .GroupJoin(
                        db.CE_Supplier,
                        ec => ec.SupplierId,
                        s => s.Id,
                        (ec, supplierGroup) => new { ec, Supplier = supplierGroup.FirstOrDefault() })
                        .GroupJoin(
                        db.CE_ContractStatuses.Where(cs => cs.Type == "Electric"),
                        combined => combined.ec.EId,
                        cs => cs.EId,
                        (combined, statusGroup) => new { combined, Status = statusGroup.FirstOrDefault() })
                        .ToListAsync();

                    // Apply dynamic filters in memory based on non-null properties in mpans
                    var filteredElectricContracts = electricContractsRaw;
                    filteredElectricContracts = filteredElectricContracts
                        .Where(x => mpans.Any(dto =>
                        x.combined.ec.MPAN == dto.MPAN
                        && (string.IsNullOrWhiteSpace(dto.BusinessName) || (!string.IsNullOrWhiteSpace(x.combined.ec.BusinessName)
                        && x.combined.ec.BusinessName.IndexOf(dto.BusinessName, StringComparison.OrdinalIgnoreCase) >= 0))
                        && (string.IsNullOrWhiteSpace(dto.InputDate) || x.combined.ec.InputDate == DateTime.ParseExact(dto.InputDate.ToString(),
                        "dd-MMM-yyyy", CultureInfo.InvariantCulture).ToString("dd/MM/yyyy"))
                        && (string.IsNullOrWhiteSpace(dto.PostCode)
                        || (!string.IsNullOrWhiteSpace(x.combined.ec.PostCode)
                        && x.combined.ec.PostCode.IndexOf(dto.PostCode, StringComparison.OrdinalIgnoreCase) >= 0))))
                        .ToList();

                    var electricContractsList = filteredElectricContracts
                        .Select(x => new
                        {
                            x.combined.ec.EId,
                            x.combined.ec.MPAN,
                            x.combined.ec.InputDate,
                            x.combined.ec.BusinessName,
                            x.combined.ec.PostCode,
                            x.combined.ec.Duration,
                            x.combined.ec.ContractNotes,
                            ContractStatus = x.Status?.ContractStatus ?? "N/A",
                            ContractType = x.Status?.Type ?? "N/A"
                        })
                        .ToList();

                    electricContracts = electricContractsList.Select(x => new RegContractSelectRowViewModel
                    {
                        EId = x.EId,
                        MPAN = x.MPAN,
                        MPRN = null,
                        InputDate = DateTime.TryParse(x.InputDate, out var dt) ? dt : (DateTime?)null,
                        BusinessName = x.BusinessName,
                        PostCode = x.PostCode,
                        Duration = x.Duration,
                        ContractStatus = x.ContractStatus,
                        ContractType = x.ContractType
                    }).ToList();
                }

                var gasContracts = new List<RegContractSelectRowViewModel>();
                // Apply MPRN filter if any MPRNs exist
                if (mprns.Any())
                {
                    // Build dynamic query for Gas Contracts
                    var gasQuery = db.CE_GasContracts.AsQueryable();
                    var mprnList = mprns.Select(x => x.MPRN).ToList();
                    gasQuery = gasQuery.Where(gc => mprnList.Contains(gc.MPRN));


                    // Apply Supplier filter
                    gasQuery = gasQuery.Where(gc => gc.SupplierId == upload.SupplierId);

                    // Execute query first, then filter in memory
                    var gasContractsRaw = await gasQuery
                        .GroupJoin(
                        db.CE_Supplier,
                        gc => gc.SupplierId,
                        s => s.Id,
                        (gc, supplierGroup) => new { gc, Supplier = supplierGroup.FirstOrDefault() })
                        .GroupJoin(
                        db.CE_ContractStatuses.Where(cs => cs.Type == "Gas"),
                        combined => combined.gc.EId,
                        cs => cs.EId,
                        (combined, statusGroup) => new { combined.gc, combined.Supplier, Status = statusGroup.FirstOrDefault() })
                        .ToListAsync();

                    // Apply dynamic filters in memory based on non-null properties in mprns
                    var filteredGasContracts = gasContractsRaw;
                    filteredGasContracts = filteredGasContracts
                        .Where(x => mprns.Any(dto =>
                        x.gc.MPRN == dto.MPRN
                        && (string.IsNullOrWhiteSpace(dto.BusinessName) || (!string.IsNullOrWhiteSpace(x.gc.BusinessName)
                        && x.gc.BusinessName.IndexOf(dto.BusinessName, StringComparison.OrdinalIgnoreCase) >= 0))
                        && (dto.InputDate != null || x.gc.InputDate == dto.InputDate.ToString())
                        && (string.IsNullOrWhiteSpace(dto.PostCode) || (!string.IsNullOrWhiteSpace(x.gc.PostCode)
                        && x.gc.PostCode.IndexOf(dto.PostCode, StringComparison.OrdinalIgnoreCase) >= 0))))
                        .ToList();

                    var gasContractsList = filteredGasContracts
                      .Select(x => new
                      {
                          x.gc.EId,
                          x.gc.MPRN,
                          x.gc.InputDate,
                          x.gc.BusinessName,
                          x.gc.PostCode,
                          x.gc.Duration,
                          x.gc.ContractNotes,
                          ContractStatus = x.Status?.ContractStatus ?? "N/A",
                          ContractType = x.Status?.Type ?? "N/A"
                      }).ToList();

                    gasContracts = gasContractsList.Select(x => new RegContractSelectRowViewModel
                    {
                        EId = x.EId,
                        MPAN = null,
                        MPRN = x.MPRN,
                        InputDate = DateTime.TryParse(x.InputDate, out var dt) ? dt : (DateTime?)null,
                        BusinessName = x.BusinessName,
                        PostCode = x.PostCode,
                        Duration = x.Duration,
                        ContractStatus = x.ContractStatus,
                        ContractType = x.ContractType
                    }).ToList();
                }

                // Combine results
                var allContracts = electricContracts.Concat(gasContracts).ToList();

                var model = new RegContractSelectListingViewModel
                {
                    UploadId = uploadId,
                    Contracts = allContracts,
                    SupplierName = supplier?.Name ?? "NULL"
                };

                return View("~/Views/PostSales/RegInvoiceSupplierDashboard/RegContractSelectListing.cshtml", model);
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
        public JsonResult ConfirmRegSelectionSupplier(List<string> selectedContracts)
        {
            try
            {
                if (selectedContracts == null || !selectedContracts.Any())
                    return JsonResponse.Fail("No contracts selected.");

                TempData["SelectedContractIds"] = selectedContracts;
                return JsonResponse.Ok(new { redirectUrl = Url.Action("EditRegContractsSupplier") });
            }
            catch (Exception ex)
            {
                Logger.Log("Error in ConfirmSelectionInvoiceSupplier: " + ex);
                return JsonResponse.Fail("Something went wrong while confirming selection. Please try again later.");
            }
        }

        [HttpGet]
        [Authorize(Roles = "Accounts,Controls")]
        public async Task<ActionResult> EditRegContractsSupplier()
        {
            try
            {
                var result = await GetRegContracts();
                if (!result.flowControl)
                {
                    return result.value;
                }

                return View("~/Views/PostSales/RegInvoiceSupplierDashboard/EditRegContractsSupplier.cshtml", result.model);
            }
            catch (Exception ex)
            {
                Logger.Log("Error in EditContractsInvoiceSupplier" + ex);
                return RedirectToAction("NotFound", "Error");
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetRegSupplierContracts()
        {
            var result = await GetRegContracts();
            if (!result.flowControl)
            {
                return (JsonResult)result.value;
            }

            return Json(new
            {
                data = result.contracts,
                contractCount = result.contracts.Count,
            }, JsonRequestBehavior.AllowGet);
        }

        private async Task<(bool flowControl, ActionResult value, List<RegContractEditRowViewModel> contracts, RegContractEditTableViewModel model)> GetRegContracts()
        {
            var selectedIds = TempData["SelectedContractIds"] as List<string>;
            if (selectedIds == null || !selectedIds.Any())
                return (flowControl: false, value: RedirectToAction("NotFound", "Error"), contracts: null, model: null);

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
                        reconciliationGroup
                    }
                )
                .GroupJoin(
                    db.CE_Supplier,
                    ec => ec.ec.SupplierId,
                    s => s.Id,
                    (ec, supplierGroup) => new
                    {
                        ec.ec,
                        ec.Status,
                        Supplier = supplierGroup.FirstOrDefault(),
                        Reconciliation = ec.reconciliationGroup.FirstOrDefault()
                    })
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
                    CED_COT = x.Reconciliation?.CED_COT ?? "N/A",
                    Supplier = x.Supplier
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
                        reconciliationGroup
                    }
                )
                .GroupJoin(
                    db.CE_Supplier,
                    ec => ec.gc.SupplierId,
                    s => s.Id,
                    (ec, supplierGroup) => new
                    {
                        ec.gc,
                        ec.Status,
                        Supplier = supplierGroup.FirstOrDefault(),
                        Reconciliation = ec.reconciliationGroup.FirstOrDefault()
                    })
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
                    CED_COT = x.Reconciliation?.CED_COT ?? "N/A",
                    Supplier = x.Supplier
                })
                .ToList();

            var electricContractsViewModel = electricContracts.Select(x => new RegContractEditRowViewModel
            {
                EId = x.ec.EId,
                MPAN = x.ec.MPAN,
                MPRN = null,
                InputDate = x.ec.InputDate,
                BusinessName = x.ec.BusinessName,
                StartDate = x.ec.InitialStartDate,
                PostCode = x.ec.PostCode,
                Duration = x.ec.Duration,
                CED = x.CED,
                CED_COT = x.CED_COT,
                ContractStatus = x.Status?.ContractStatus ?? "N/A",
                ContractType = x.Status?.Type ?? "N/A",
                ContractNotes = x.ec.ContractNotes,
                SupplierId = x.ec.SupplierId,
                SupplierName = x.Supplier?.Name
            });

            var gasContractsViewModel = gasContracts.Select(x => new RegContractEditRowViewModel
            {
                EId = x.gc.EId,
                MPAN = null,
                MPRN = x.gc.MPRN,
                InputDate = x.gc.InputDate,
                BusinessName = x.gc.BusinessName,
                StartDate = x.gc.InitialStartDate,
                PostCode = x.gc.PostCode,
                Duration = x.gc.Duration,
                CED = x.CED,
                CED_COT = x.CED_COT,
                ContractStatus = x.Status?.ContractStatus ?? "N/A",
                ContractType = x.Status?.Type ?? "N/A",
                ContractNotes = x.gc.ContractNotes,
                SupplierId = x.gc.SupplierId,
                SupplierName = x.Supplier?.Name
            });

            var contracts = electricContractsViewModel.Concat(gasContractsViewModel).ToList();
            var model = new RegContractEditTableViewModel
            {
                Contracts = contracts
            };

            return (flowControl: true, value: null, contracts: contracts, model: model);
        }

        #endregion

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> UpdatePostSalesRow(UpdateRegPostSalesFieldDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.EId))
                return Json(new { success = false, message = "Invalid payload" });

            bool updated = false;

            try
            {
                // 1. Update Contract Email
                updated |= await UpdateContractDates(dto);

                // 2. Contract Status
                updated |= await UpdateContractStatus(dto);

                // 3. Reconciliation (insert or update)
                var cr = await UpsertReconciliation(dto);
                updated |= cr != null;

                // 4. Always insert StatusDashboardLogs (new log entry)
                updated |= InsertStatusDashboardLog(dto, User.Identity?.Name ?? "System");

                if (updated)
                {
                    await db.SaveChangesAsync();
                    return JsonResponse.Ok(message: "Contract updated successfully.");
                }

                return JsonResponse.Ok(message: "No update applied");
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        private async Task<bool> UpdateContractDates(UpdateRegPostSalesFieldDto dto)
        {
            if (dto.ContractType == "Electric")
            {
                var contract = await db.CE_ElectricContracts
                    .FirstOrDefaultAsync(x => x.EId == dto.EId);

                if (contract != null)
                {
                    contract.InitialStartDate = dto.StartDate;
                    return true;
                }
            }
            else
            {
                var contract = await db.CE_GasContracts
                    .FirstOrDefaultAsync(x => x.EId == dto.EId);

                if (contract != null)
                {
                    contract.InitialStartDate = dto.StartDate;
                    return true;
                }
            }

            return false;
        }

        private async Task<CE_CommissionAndReconciliation> UpsertReconciliation(UpdateRegPostSalesFieldDto dto)
        {
            var cr = await db.CE_CommissionAndReconciliation
                .Include(x => x.Metrics)
                .FirstOrDefaultAsync(c => c.EId == dto.EId && c.contractType == dto.ContractType);

            if (cr == null)
            {
                cr = new CE_CommissionAndReconciliation
                {
                    EId = dto.EId,
                    contractType = dto.ContractType
                };
                db.CE_CommissionAndReconciliation.Add(cr);
            }

            if (DateTime.TryParse(dto.StartDate, out var sd))
                sd.ToString("yyyy-mm-dd");

            if (DateTime.TryParse(dto.CED, out var ced))
                cr.CED = ced.ToString("yyyy-MM-dd");
            else if (!string.IsNullOrWhiteSpace(dto.StartDate) && int.TryParse(dto.Duration, out var durYears))
                cr.CED = sd.AddYears(durYears).AddDays(-1).ToString("yyyy-MM-dd");

            if (DateTime.TryParse(dto.COTDate, out var cot))
                cr.CED_COT = cot.ToString("yyyy-MM-dd");

            return cr;
        }

        private async Task<bool> UpdateContractStatus(UpdateRegPostSalesFieldDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ContractStatus)) return false;

            var status = await db.CE_ContractStatuses
                .FirstOrDefaultAsync(s => s.EId == dto.EId && s.Type == dto.ContractType);

            if (status != null)
            {
                status.ContractStatus = dto.ContractStatus;
                status.ModifyDate = DateTime.Now;
                return true;
            }

            return false;
        }

        private bool InsertStatusDashboardLog(UpdateRegPostSalesFieldDto dto, string userName)
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
                CreatedBy = userName,
                CreatedDate = DateTime.Now
            };

            db.CE_PostSalesLogs.Add(log);
            return true;
        }
    }
}