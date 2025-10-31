using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Filters;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.Sector;
using CobanaEnergy.Project.Models.Sector.Brokerage;
using CobanaEnergy.Project.Models.Sector.Commissions;
using CobanaEnergy.Project.Models.Sector.Common;
using CobanaEnergy.Project.Models.Sector.Introducer;
using CobanaEnergy.Project.Models.Sector.ReferralPartner;
using CobanaEnergy.Project.Models.Sector.SectorDBModels;
using Logic;
using Logic.ResponseModel.Helper;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Controllers.Sector
{
    /// <summary>
    /// Handles Sector management functionality
    /// </summary>
    public class SectorController : BaseController
    {
        private readonly ApplicationDBContext db;

        public SectorController(ApplicationDBContext _db)
        {
            db = _db;
        }

        #region SectorDashboard

        [HttpGet]
        [Authorize(Roles = "Controls")]
        public async Task<ActionResult> Dashboard()
        {
            try
            {
                var sectors = await db.CE_Sector
                  .Include("SectorSuppliers.Supplier")
                  .OrderByDescending(s => s.SectorID)
                  .ToListAsync();

                var sectorItems = sectors.Select(s => new SectorItemViewModel
                {
                    SectorId = s.SectorID.ToString(),
                    Name = s.Name,
                    Active = s.Active,
                    CreatedDate = s.StartDate?.ToString("dd-MM-yy") ?? "",
                    Mobile = s.Mobile ?? "",
                    SectorType = s.SectorType,
                    Suppliers = s.SectorSuppliers?.Select(ss => ss.Supplier?.Name).Where(name => !string.IsNullOrEmpty(name)).ToList() ?? new List<string>()
                }).ToList();

                var model = new SectorDashboardViewModel
                {
                    Sectors = sectorItems,
                    TotalSectors = sectorItems.Count,
                    ActiveSectors = sectorItems.Count(s => s.Active),
                    InactiveSectors = sectorItems.Count(s => !s.Active)
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"Failed to load sector dashboard: {ex.Message}";
                TempData["ToastType"] = "error";
                Logic.Logger.Log($"Sector Dashboard failed: {ex.Message}");
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public async Task<ActionResult> AllDashboard()
        {
            try
            {
                var sectors = await db.CE_Sector
                   .Include("SectorSuppliers.Supplier")
                   .OrderByDescending(s => s.SectorID)
                   .ToListAsync();

                var sectorItems = sectors.Select(s => new SectorItemViewModel
                {
                    SectorId = s.SectorID.ToString(),
                    Name = s.Name,
                    Active = s.Active,
                    CreatedDate = s.StartDate?.ToString("dd-MM-yy") ?? "",
                    Mobile = s.Mobile ?? "",
                    SectorType = s.SectorType,
                    Suppliers = s.SectorSuppliers?.Select(ss => ss.Supplier?.Name).Where(name => !string.IsNullOrEmpty(name)).ToList() ?? new List<string>()
                }).ToList();

                var model = new SectorDashboardViewModel
                {
                    Sectors = sectorItems,
                    TotalSectors = sectorItems.Count,
                    ActiveSectors = sectorItems.Count(s => s.Active),
                    InactiveSectors = sectorItems.Count(s => !s.Active)
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"Failed to load sector dashboard: {ex.Message}";
                TempData["ToastType"] = "error";
                Logic.Logger.Log($"Sector Dashboard failed: {ex.Message}");
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        [Authorize(Roles = "Controls")]
        public async Task<JsonResult> GetSectorsByType(string sectorType)
        {
            try
            {
                // Only use AJAX for filtering when user actually changes the filter
                if (string.IsNullOrEmpty(sectorType))
                {
                    return JsonResponse.Fail("Sector type is required for filtering.");
                }

                var sectors = await db.CE_Sector
                    .Include("SectorSuppliers.Supplier")
                    .Where(s => s.SectorType == sectorType)
                    .OrderByDescending(s => s.SectorID)
                    .ToListAsync();

                var sectorItems = sectors.Select(s => new SectorItemViewModel
                {
                    SectorId = s.SectorID.ToString(),
                    Name = s.Name,
                    Active = s.Active,
                    CreatedDate = s.StartDate?.ToString("dd-MM-yy") ?? "",
                    Mobile = s.Mobile ?? "",
                    SectorType = s.SectorType,
                    Suppliers = s.SectorSuppliers?.Select(ss => ss.Supplier?.Name).Where(name => !string.IsNullOrEmpty(name)).ToList() ?? new List<string>()
                }).ToList();

                // Return just the filtered data, not the full model
                return JsonResponse.Ok(new
                {
                    Sectors = sectorItems,
                    TotalSectors = sectorItems.Count,
                    ActiveSectors = sectorItems.Count(s => s.Active),
                    InactiveSectors = sectorItems.Count(s => !s.Active)
                });
            }
            catch (Exception ex)
            {
                Logic.Logger.Log($"Failed to load sectors by type: {ex.Message}");
                return JsonResponse.Fail("Failed to load sectors.");
            }
        }

        /// <summary>
        /// Get active sectors by type for contract forms (public access)
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> GetActiveSectors(string sectorType)
        {
            try
            {
                if (string.IsNullOrEmpty(sectorType))
                {
                    return JsonResponse.Fail("Sector type is required.");
                }

                if (sectorType == "Referral")
                {
                    sectorType = "Referral Partner";
                }

                var currentDate = DateTime.Today; // Get current date without time

                var sectors = await db.CE_Sector
                    .Where(s => s.SectorType == sectorType &&
                               s.Active &&
                               s.StartDate <= currentDate &&
                               (s.EndDate == null || s.EndDate > currentDate))
                    .OrderBy(s => s.Name)
                    .Select(s => new
                    {
                        SectorId = s.SectorID,
                        Name = s.Name,
                        OfgemID = s.OfgemID,
                        Department = s.Department
                    })
                    .ToListAsync();

                return JsonResponse.Ok(new { Sectors = sectors });
            }
            catch (Exception ex)
            {
                Logic.Logger.Log($"Failed to load active sectors of type {sectorType}: {ex.Message}");
                return JsonResponse.Fail($"Failed to load {sectorType} sectors.");
            }
        }

        /// <summary>
        /// Get active sub-sectors by type for contract forms (public access)
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> GetActiveSubSectors(string subSectorType, int? sectorId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(subSectorType))
                {
                    return JsonResponse.Fail("Sub-sector type is required.");
                }

                var currentDate = DateTime.Today; // Get current date without time
                List<object> subSectors = new List<object>();

                switch (subSectorType.ToLower())
                {
                    case "subreferral":
                        var subReferrals = await db.CE_SubReferral
                            .Where(s => s.Active &&
                                       s.StartDate <= currentDate &&
                                       (s.EndDate == null || s.EndDate > currentDate))
                            .OrderBy(s => s.SubReferralPartnerName)
                            .Select(s => new { SubSectorId = s.SubReferralID, Name = s.SubReferralPartnerName })
                            .ToListAsync();
                        subSectors.AddRange(subReferrals);
                        break;

                    case "subintroducer":
                        var subIntroducers = await db.CE_SubIntroducer
                            .Where(s => s.Active &&
                                       s.StartDate <= currentDate &&
                                       (s.EndDate == null || s.EndDate > currentDate))
                            .OrderBy(s => s.SubIntroducerName)
                            .Select(s => new { SubSectorId = s.SubIntroducerID, Name = s.SubIntroducerName })
                            .ToListAsync();
                        subSectors.AddRange(subIntroducers);
                        break;

                    case "subbrokerage":
                        var subBrokerages = await db.CE_SubBrokerage
                            .Where(s => s.Active &&
                                       s.StartDate <= currentDate &&
                                       (s.EndDate == null || s.EndDate > currentDate))
                            .OrderBy(s => s.SubBrokerageName)
                            .Select(s => new { SubSectorId = s.SubBrokerageID, Name = s.SubBrokerageName })
                            .ToListAsync();
                        subSectors.AddRange(subBrokerages);
                        break;

                    case "brokeragestaff":
                        var brokerageStaff = await db.CE_BrokerageStaff
                            .Where(s => s.Active &&
                                       (sectorId == null || s.SectorID == sectorId.Value) &&
                                       s.StartDate <= currentDate &&
                                       (s.EndDate == null || s.EndDate > currentDate))
                            .OrderBy(s => s.BrokerageStaffName)
                            .Select(s => new { SubSectorId = s.BrokerageStaffID, Name = s.BrokerageStaffName })
                            .ToListAsync();
                        subSectors.AddRange(brokerageStaff);
                        break;

                    default:
                        return JsonResponse.Fail($"Unknown sub-sector type: {subSectorType}");
                }

                return JsonResponse.Ok(new { SubSectors = subSectors });
            }
            catch (Exception ex)
            {
                Logic.Logger.Log($"Failed to load active sub-sectors of type {subSectorType}: {ex.Message}");
                return JsonResponse.Fail($"Failed to load {subSectorType} sub-sectors.");
            }
        }

        [HttpGet]
        [Authorize(Roles = "Controls")]
        public async Task<JsonResult> GetSectorStatistics()
        {
            try
            {
                var totalSectors = await db.CE_Sector.CountAsync();
                var activeSectors = await db.CE_Sector.CountAsync(s => s.Active);
                var inactiveSectors = await db.CE_Sector.CountAsync(s => !s.Active);

                var sectorTypeStats = await db.CE_Sector
                    .GroupBy(s => s.SectorType)
                    .Select(g => new
                    {
                        SectorType = g.Key,
                        Count = g.Count(),
                        ActiveCount = g.Count(s => s.Active)
                    })
                    .ToListAsync();

                var stats = new
                {
                    TotalSectors = totalSectors,
                    ActiveSectors = activeSectors,
                    InactiveSectors = inactiveSectors,
                    SectorTypeBreakdown = sectorTypeStats
                };

                return JsonResponse.Ok(stats);
            }
            catch (Exception ex)
            {
                Logic.Logger.Log($"Failed to load sector statistics: {ex.Message}");
                return JsonResponse.Fail("Failed to load sector statistics.");
            }
        }

        [HttpGet]
        [Authorize(Roles = "Controls")]
        public async Task<JsonResult> RefreshDashboardData()
        {
            try
            {
                // This method can be called after CRUD operations to refresh the dashboard
                var sectors = await db.CE_Sector
                    .OrderByDescending(s => s.SectorID)
                    .ToListAsync();

                var sectorItems = sectors.Select(s => new SectorItemViewModel
                {
                    SectorId = s.SectorID.ToString(),
                    Name = s.Name,
                    Active = s.Active,
                    CreatedDate = s.StartDate?.ToString("dd-MM-yy") ?? "",
                    Mobile = s.Mobile ?? "",
                    SectorType = s.SectorType
                }).ToList();

                var model = new SectorDashboardViewModel
                {
                    Sectors = sectorItems,
                    TotalSectors = sectorItems.Count,
                    ActiveSectors = sectorItems.Count(s => s.Active),
                    InactiveSectors = sectorItems.Count(s => !s.Active)
                };

                return JsonResponse.Ok(model);
            }
            catch (Exception ex)
            {
                Logic.Logger.Log($"Failed to refresh dashboard data: {ex.Message}");
                return JsonResponse.Fail("Failed to refresh dashboard data.");
            }
        }

        #endregion

        #region CreateSector

        [HttpGet]
        [Authorize(Roles = "Controls")]
        public ActionResult Create()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"Failed to load create sector page: {ex.Message}";
                TempData["ToastType"] = "error";
                Logic.Logger.Log($"Create Sector page rendering failed!! {ex.Message}");
                return RedirectToAction("Dashboard", "Sector");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Controls")]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> Create(CreateSectorViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(x => x.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new
                {
                    success = false,
                    message = "Validation failed. Please check your input.",
                    errors = errors
                }, JsonRequestBehavior.AllowGet);
            }

            // Additional validation for Brokerage and Introducers
            if ((model.SectorType == "Brokerage" || model.SectorType == "Introducer") &&
                string.IsNullOrWhiteSpace(model.OfgemID))
            {
                return Json(new
                {
                    success = false,
                    message = "Ofgem ID is required for Brokerage and Introducer sectors.",
                    errors = new[] { "Ofgem ID is required for Brokerage and Introducer sectors." }
                }, JsonRequestBehavior.AllowGet);
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // Step 1: Create the main sector
                    var sector = new CE_Sector
                    {
                        Name = model.Name,
                        Active = model.Active,
                        StartDate = !string.IsNullOrEmpty(model.StartDate) ? DateTime.Parse(model.StartDate) : (DateTime?)null,
                        EndDate = DateTime.MaxValue,
                        Email = model.Email,
                        Landline = model.Landline,
                        Mobile = model.Mobile,
                        OfgemID = model.OfgemID,
                        Department = model.Department,
                        SectorType = model.SectorType
                    };

                    db.CE_Sector.Add(sector);
                    await db.SaveChangesAsync();

                    // Step 2: Add Bank Details if provided
                    if (model.BankDetails != null && !string.IsNullOrEmpty(model.BankDetails.BankName))
                    {
                        var bankDetails = new CE_BankDetails
                        {
                            EntityType = "Sector",
                            EntityID = sector.SectorID,
                            BankName = model.BankDetails.BankName,
                            BankBranchAddress = model.BankDetails.BankBranchAddress,
                            ReceiversAddress = model.BankDetails.ReceiversAddress,
                            AccountName = model.BankDetails.AccountName,
                            AccountSortCode = model.BankDetails.AccountSortCode,
                            AccountNumber = model.BankDetails.AccountNumber,
                            IBAN = model.BankDetails.IBAN,
                            SwiftCode = model.BankDetails.SwiftCode
                        };
                        db.CE_BankDetails.Add(bankDetails);
                    }

                    await db.SaveChangesAsync();

                    // Step 3: Add Company Tax Info if provided
                    if (model.CompanyTaxInfo != null && !string.IsNullOrEmpty(model.CompanyTaxInfo.CompanyRegistration))
                    {
                        var companyTaxInfo = new CE_CompanyTaxInfo
                        {
                            EntityType = "Sector",
                            EntityID = sector.SectorID,
                            CompanyRegistration = model.CompanyTaxInfo.CompanyRegistration,
                            VATNumber = model.CompanyTaxInfo.VATNumber,
                            Notes = model.CompanyTaxInfo.Notes
                        };
                        db.CE_CompanyTaxInfo.Add(companyTaxInfo);
                    }
                    await db.SaveChangesAsync();


                    // Step 4: Add Commission and Payment records based on sector type
                    await AddCommissionRecords(sector.SectorID, model);

                    // Step 5: Add Staff and Sub-sections based on sector type
                    await AddStaffAndSubsections(sector.SectorID, model);

                    // Step 6: Handle Sector Suppliers (for Brokerage sector type)
                    await HandleSectorSuppliers(sector.SectorID, model.SectorSuppliers);

                    // Step 7: Add Bank Details and Company Tax Info for sub-entities (MOVED to AddStaffAndSubsections to fix EntityID=1 bug)
                    // await AddSubEntityBankDetailsAndTaxInfo(model);

                    await db.SaveChangesAsync();
                    transaction.Commit();

                    // Return JSON for immediate toast notification
                    return Json(new
                    {
                        success = true,
                        message = "Sector created successfully!",
                        redirectUrl = Url.Action("Dashboard", "Sector")
                    }, JsonRequestBehavior.AllowGet);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Logic.Logger.Log($"Sector creation failed: {ex.Message}");

                    // Return JSON error for immediate toast notification
                    return Json(new
                    {
                        success = false,
                        message = "An unexpected error occurred while saving sector.",
                        errors = new[] { ex.Message }
                    }, JsonRequestBehavior.AllowGet);
                }
            }
        }

        private async Task AddCommissionRecords(int sectorId, CreateSectorViewModel model)
        {
            // Add Brokerage Commissions
            if (model.BrokerageCommissions != null && model.BrokerageCommissions.Any(c => !string.IsNullOrEmpty(c.Commission.ToString())))
            {
                foreach (var commission in model.BrokerageCommissions.Where(c => c != null && !string.IsNullOrEmpty(c.Commission.ToString())))
                {
                    var brokerageCommission = new CE_BrokerageCommissionAndPayment
                    {
                        SectorID = sectorId,
                        Commission = commission.Commission,  // ViewModel.Commission -> DBModel.Commission
                        StartDate = !string.IsNullOrEmpty(commission.StartDate) ? DateTime.Parse(commission.StartDate) : DateTime.Now,
                        EndDate = DateTime.MaxValue,
                        PaymentTerms = commission.PaymentTerms ?? "",
                        CommissionType = commission.CommissionType ?? ""
                    };
                    db.CE_BrokerageCommissionAndPayment.Add(brokerageCommission);
                }
            }

            // Add Closer Commissions
            if (model.CloserCommissions != null && model.CloserCommissions.Any(c => !string.IsNullOrEmpty(c.Commission.ToString())))
            {
                foreach (var commission in model.CloserCommissions.Where(c => c != null && !string.IsNullOrEmpty(c.Commission.ToString())))
                {
                    var closerCommission = new CE_CloserCommissionAndPayment
                    {
                        SectorID = sectorId,
                        Commission = commission.Commission,
                        StartDate = !string.IsNullOrEmpty(commission.StartDate) ? DateTime.Parse(commission.StartDate) : DateTime.Now,
                        EndDate = DateTime.MaxValue,
                        PaymentTerms = commission.PaymentTerms ?? "",
                        CommissionType = commission.CommissionType ?? ""
                    };
                    db.CE_CloserCommissionAndPayment.Add(closerCommission);
                }
            }

            // Add Introducer Commissions
            if (model.IntroducerCommissions != null && model.IntroducerCommissions.Any(c => !string.IsNullOrEmpty(c.Commission.ToString())))
            {
                foreach (var commission in model.IntroducerCommissions.Where(c => c != null && !string.IsNullOrEmpty(c.Commission.ToString())))
                {
                    var introducerCommission = new CE_IntroducerCommissionAndPayment
                    {
                        SectorID = sectorId,
                        CommissionPercent = commission.Commission,
                        StartDate = !string.IsNullOrEmpty(commission.StartDate) ? DateTime.Parse(commission.StartDate) : DateTime.Now,
                        EndDate = DateTime.MaxValue,
                        PaymentTerms = commission.PaymentTerms,
                        CommissionType = commission.CommissionType
                    };
                    db.CE_IntroducerCommissionAndPayment.Add(introducerCommission);
                }
            }

            // Add Referral Partner Commissions
            if (model.ReferralPartnerCommissions != null && model.ReferralPartnerCommissions.Any(c => !string.IsNullOrEmpty(c.ReferralPartnerCommission.ToString())))
            {
                foreach (var commission in model.ReferralPartnerCommissions.Where(c => c != null && !string.IsNullOrEmpty(c.ReferralPartnerCommission.ToString())))
                {
                    var referralCommission = new CE_ReferralPartnerCommissionAndPayment
                    {
                        SectorID = sectorId,
                        ReferralPartnerCommission = commission.ReferralPartnerCommission,
                        ReferralPartnerStartDate = !string.IsNullOrEmpty(commission.ReferralPartnerStartDate) ? DateTime.Parse(commission.ReferralPartnerStartDate) : (DateTime?)null,
                        ReferralPartnerEndDate = DateTime.MaxValue,
                        BrokerageCommission = commission.BrokerageCommission,
                        BrokerageStartDate = !string.IsNullOrEmpty(commission.BrokerageStartDate) ? DateTime.Parse(commission.BrokerageStartDate) : (DateTime?)null,
                        BrokerageEndDate = DateTime.MaxValue,
                        PaymentTerms = commission.PaymentTerms,
                        CommissionType = commission.CommissionType
                    };
                    db.CE_ReferralPartnerCommissionAndPayment.Add(referralCommission);
                }
            }

            // Add Lead Generator Commissions
            if (model.LeadGeneratorCommissions != null && model.LeadGeneratorCommissions.Any(c => !string.IsNullOrEmpty(c.LeadGeneratorCommission.ToString())))
            {
                foreach (var commission in model.LeadGeneratorCommissions.Where(c => c != null && !string.IsNullOrEmpty(c.LeadGeneratorCommission.ToString())))
                {
                    var leadGenCommission = new CE_LeadGeneratorCommissionAndPayment
                    {
                        SectorID = sectorId,
                        LeadGeneratorCommissionPercent = commission.LeadGeneratorCommission,
                        LeadGeneratorStartDate = !string.IsNullOrEmpty(commission.LeadGeneratorStartDate) ? DateTime.Parse(commission.LeadGeneratorStartDate) : (DateTime?)null,
                        LeadGeneratorEndDate = DateTime.MaxValue,
                        CloserCommissionPercent = commission.CloserCommission,
                        CloserStartDate = !string.IsNullOrEmpty(commission.CloserStartDate) ? DateTime.Parse(commission.CloserStartDate) : (DateTime?)null,
                        CloserEndDate = DateTime.MaxValue,
                        PaymentTerms = commission.PaymentTerms,
                        CommissionType = commission.CommissionType
                    };
                    db.CE_LeadGeneratorCommissionAndPayment.Add(leadGenCommission);
                }
            }
        }

        private async Task AddStaffAndSubsections(int sectorId, CreateSectorViewModel model)
        {
            // Add Brokerage Staff
            if (model.BrokerageStaff != null && model.BrokerageStaff.Any(s => !string.IsNullOrEmpty(s.BrokerageStaffName)))
            {
                foreach (var staff in model.BrokerageStaff.Where(s => s != null && !string.IsNullOrEmpty(s.BrokerageStaffName)))
                {
                    var brokerageStaff = new CE_BrokerageStaff
                    {
                        SectorID = sectorId,
                        BrokerageStaffName = staff.BrokerageStaffName,
                        Active = staff.Active,
                        StartDate = !string.IsNullOrEmpty(staff.StartDate) ? DateTime.Parse(staff.StartDate) : (DateTime?)null,
                        EndDate = DateTime.MaxValue,
                        Email = staff.Email ?? "",
                        Landline = staff.Landline ?? "",
                        Mobile = staff.Mobile ?? "",
                        Notes = staff.Notes ?? ""
                    };
                    db.CE_BrokerageStaff.Add(brokerageStaff);
                }
            }

            // Add Sub Brokerages and their commissions
            if (model.SubBrokerages != null && model.SubBrokerages.Any(s => !string.IsNullOrEmpty(s.SubBrokerageName)))
            {
                foreach (var subBrokerage in model.SubBrokerages.Where(s => s != null && !string.IsNullOrEmpty(s.SubBrokerageName)))
                {
                    var subBrokerageEntity = new CE_SubBrokerage
                    {
                        SectorID = sectorId,
                        SubBrokerageName = subBrokerage.SubBrokerageName,
                        OfgemID = subBrokerage.OfgemID ?? "",
                        Active = subBrokerage.Active,
                        StartDate = !string.IsNullOrEmpty(subBrokerage.StartDate) ? DateTime.Parse(subBrokerage.StartDate) : (DateTime?)null,
                        EndDate = DateTime.MaxValue,
                        Email = subBrokerage.Email ?? "",
                        Landline = subBrokerage.Landline ?? "",
                        Mobile = subBrokerage.Mobile ?? ""
                    };
                    db.CE_SubBrokerage.Add(subBrokerageEntity);

                    // Save immediately to get the ID for commission records
                    await db.SaveChangesAsync();

                    // Add Sub Brokerage Commissions using the actual ID
                    if (subBrokerage.Commissions != null && subBrokerage.Commissions.Any(c => !string.IsNullOrEmpty(c.SubBrokerageCommission.ToString())))
                    {
                        foreach (var commission in subBrokerage.Commissions.Where(c => c != null && !string.IsNullOrEmpty(c.SubBrokerageCommission.ToString())))
                        {
                            var subBrokerageCommission = new CE_SubBrokerageCommissionAndPayment
                            {
                                SubBrokerageID = subBrokerageEntity.SubBrokerageID, // Use actual ID, not 0
                                SubBrokerageCommissionPercent = commission.SubBrokerageCommission,
                                SubBrokerageStartDate = !string.IsNullOrEmpty(commission.SubBrokerageStartDate) ? DateTime.Parse(commission.SubBrokerageStartDate) : (DateTime?)null,
                                SubBrokerageEndDate = DateTime.MaxValue,
                                BrokerageCommissionPercent = commission.BrokerageCommission,
                                BrokerageStartDate = !string.IsNullOrEmpty(commission.BrokerageStartDate) ? DateTime.Parse(commission.BrokerageStartDate) : (DateTime?)null,
                                BrokerageEndDate = DateTime.MaxValue,
                                PaymentTerms = commission.PaymentTerms ?? "",
                                CommissionType = commission.CommissionType ?? ""
                            };
                            db.CE_SubBrokerageCommissionAndPayment.Add(subBrokerageCommission);
                        }
                    }

                    // Add Bank Details if provided (move from AddSubEntityBankDetailsAndTaxInfo to fix EntityID=1 bug)
                    if (subBrokerage.BankDetails != null && !string.IsNullOrEmpty(subBrokerage.BankDetails.BankName))
                    {
                        var subBrokerageBankDetails = new CE_BankDetails
                        {
                            EntityType = "SubBrokerage",
                            EntityID = subBrokerageEntity.SubBrokerageID, // Use the correct ID from the created entity
                            BankName = subBrokerage.BankDetails.BankName,
                            BankBranchAddress = subBrokerage.BankDetails.BankBranchAddress,
                            ReceiversAddress = subBrokerage.BankDetails.ReceiversAddress,
                            AccountName = subBrokerage.BankDetails.AccountName,
                            AccountSortCode = subBrokerage.BankDetails.AccountSortCode,
                            AccountNumber = subBrokerage.BankDetails.AccountNumber,
                            IBAN = subBrokerage.BankDetails.IBAN,
                            SwiftCode = subBrokerage.BankDetails.SwiftCode
                        };
                        db.CE_BankDetails.Add(subBrokerageBankDetails);
                    }

                    // Add Company Tax Info if provided (move from AddSubEntityBankDetailsAndTaxInfo to fix EntityID=1 bug)
                    if (subBrokerage.CompanyTaxInfo != null && !string.IsNullOrEmpty(subBrokerage.CompanyTaxInfo.CompanyRegistration))
                    {
                        var subBrokerageCompanyTaxInfo = new CE_CompanyTaxInfo
                        {
                            EntityType = "SubBrokerage",
                            EntityID = subBrokerageEntity.SubBrokerageID, // Use the correct ID from the created entity
                            CompanyRegistration = subBrokerage.CompanyTaxInfo.CompanyRegistration,
                            VATNumber = subBrokerage.CompanyTaxInfo.VATNumber,
                            Notes = subBrokerage.CompanyTaxInfo.Notes
                        };
                        db.CE_CompanyTaxInfo.Add(subBrokerageCompanyTaxInfo);
                    }
                }
            }

            // Add Sub Referrals and their commissions
            if (model.SubReferrals != null && model.SubReferrals.Any(s => !string.IsNullOrEmpty(s.SubReferralPartnerName)))
            {
                foreach (var subReferral in model.SubReferrals.Where(s => s != null && !string.IsNullOrEmpty(s.SubReferralPartnerName)))
                {
                    var subReferralEntity = new CE_SubReferral
                    {
                        SectorID = sectorId,
                        SubReferralPartnerName = subReferral.SubReferralPartnerName,
                        Active = subReferral.Active,
                        StartDate = !string.IsNullOrEmpty(subReferral.StartDate) ? DateTime.Parse(subReferral.StartDate) : (DateTime?)null,
                        EndDate = DateTime.MaxValue,
                        SubReferralPartnerEmail = subReferral.Email ?? "",
                        SubReferralPartnerLandline = subReferral.Landline ?? "",
                        SubReferralPartnerMobile = subReferral.Mobile ?? ""
                    };
                    db.CE_SubReferral.Add(subReferralEntity);

                    // Save immediately to get the ID for commission records
                    await db.SaveChangesAsync();

                    // Add Sub Referral Commissions using the actual ID
                    if (subReferral.Commissions != null && subReferral.Commissions.Any(c => !string.IsNullOrEmpty(c.SubReferralCommission.ToString())))
                    {
                        foreach (var commission in subReferral.Commissions.Where(c => c != null && !string.IsNullOrEmpty(c.SubReferralCommission.ToString())))
                        {
                            var subReferralCommission = new CE_SubReferralCommissionAndPayment
                            {
                                SubReferralID = subReferralEntity.SubReferralID, // Use actual ID, not 0
                                SubIntroducerCommission = commission.SubReferralCommission,
                                SubIntroducerStartDate = !string.IsNullOrEmpty(commission.SubReferralStartDate) ? DateTime.Parse(commission.SubReferralStartDate) : (DateTime?)null,
                                SubIntroducerEndDate = DateTime.MaxValue,
                                IntroducerCommission = commission.ReferralPartnerCommission,
                                IntroducerStartDate = !string.IsNullOrEmpty(commission.ReferralPartnerStartDate) ? DateTime.Parse(commission.ReferralPartnerStartDate) : (DateTime?)null,
                                IntroducerEndDate = DateTime.MaxValue,
                                PaymentTerms = commission.PaymentTerms ?? "",
                                CommissionType = commission.CommissionType ?? ""
                            };
                            db.CE_SubReferralCommissionAndPayment.Add(subReferralCommission);
                        }
                    }

                    // Add Bank Details if provided (move from AddSubEntityBankDetailsAndTaxInfo to fix EntityID=1 bug)
                    if (subReferral.BankDetails != null && !string.IsNullOrEmpty(subReferral.BankDetails.BankName))
                    {
                        var subReferralBankDetails = new CE_BankDetails
                        {
                            EntityType = "SubReferral",
                            EntityID = subReferralEntity.SubReferralID, // Use the correct ID from the created entity
                            BankName = subReferral.BankDetails.BankName,
                            BankBranchAddress = subReferral.BankDetails.BankBranchAddress,
                            ReceiversAddress = subReferral.BankDetails.ReceiversAddress,
                            AccountName = subReferral.BankDetails.AccountName,
                            AccountSortCode = subReferral.BankDetails.AccountSortCode,
                            AccountNumber = subReferral.BankDetails.AccountNumber,
                            IBAN = subReferral.BankDetails.IBAN,
                            SwiftCode = subReferral.BankDetails.SwiftCode
                        };
                        db.CE_BankDetails.Add(subReferralBankDetails);
                    }

                    // Add Company Tax Info if provided (move from AddSubEntityBankDetailsAndTaxInfo to fix EntityID=1 bug)
                    if (subReferral.CompanyTaxInfo != null && !string.IsNullOrEmpty(subReferral.CompanyTaxInfo.CompanyRegistration))
                    {
                        var subReferralCompanyTaxInfo = new CE_CompanyTaxInfo
                        {
                            EntityType = "SubReferral",
                            EntityID = subReferralEntity.SubReferralID, // Use the correct ID from the created entity
                            CompanyRegistration = subReferral.CompanyTaxInfo.CompanyRegistration,
                            VATNumber = subReferral.CompanyTaxInfo.VATNumber,
                            Notes = subReferral.CompanyTaxInfo.Notes
                        };
                        db.CE_CompanyTaxInfo.Add(subReferralCompanyTaxInfo);
                    }
                }
            }

            // Add Sub Introducers and their commissions
            if (model.SubIntroducers != null && model.SubIntroducers.Any(s => !string.IsNullOrEmpty(s.SubIntroducerName)))
            {
                foreach (var subIntroducer in model.SubIntroducers.Where(s => s != null && !string.IsNullOrEmpty(s.SubIntroducerName)))
                {
                    var subIntroducerEntity = new CE_SubIntroducer
                    {
                        SectorID = sectorId,
                        SubIntroducerName = subIntroducer.SubIntroducerName,
                        OfgemID = subIntroducer.OfgemID ?? "",
                        Active = subIntroducer.Active,
                        StartDate = !string.IsNullOrEmpty(subIntroducer.StartDate) ? DateTime.Parse(subIntroducer.StartDate) : (DateTime?)null,
                        EndDate = DateTime.MaxValue,
                        SubIntroducerEmail = subIntroducer.Email ?? "",
                        SubIntroducerLandline = subIntroducer.Landline ?? "",
                        SubIntroducerMobile = subIntroducer.Mobile ?? ""
                    };
                    db.CE_SubIntroducer.Add(subIntroducerEntity);

                    // Save immediately to get the ID for commission records
                    await db.SaveChangesAsync();

                    // Add Sub Introducer Commissions using the actual ID
                    if (subIntroducer.Commissions != null && subIntroducer.Commissions.Any(c => !string.IsNullOrEmpty(c.SubIntroducerCommission.ToString())))
                    {
                        foreach (var commission in subIntroducer.Commissions.Where(c => c != null && !string.IsNullOrEmpty(c.SubIntroducerCommission.ToString())))
                        {
                            var subIntroducerCommission = new CE_SubIntroducerCommissionAndPayment
                            {
                                SubIntroducerID = subIntroducerEntity.SubIntroducerID, // Use actual ID, not 0
                                SubIntroducerCommission = commission.SubIntroducerCommission,
                                SubIntroducerCommissionStartDate = !string.IsNullOrEmpty(commission.SubIntroducerStartDate) ? DateTime.Parse(commission.SubIntroducerStartDate) : (DateTime?)null,
                                SubIntroducerCommissionEndDate = DateTime.MaxValue,
                                IntroducerCommission = commission.IntroducerCommission,
                                IntroducerCommissionStartDate = !string.IsNullOrEmpty(commission.IntroducerStartDate) ? DateTime.Parse(commission.IntroducerStartDate) : (DateTime?)null,
                                IntroducerCommissionEndDate = DateTime.MaxValue,
                                PaymentTerms = commission.PaymentTerms ?? "",
                                CommissionType = commission.CommissionType ?? ""
                            };
                            db.CE_SubIntroducerCommissionAndPayment.Add(subIntroducerCommission);
                        }
                    }

                    // Add Bank Details if provided (move from AddSubEntityBankDetailsAndTaxInfo to fix EntityID=1 bug)
                    if (subIntroducer.BankDetails != null && !string.IsNullOrEmpty(subIntroducer.BankDetails.BankName))
                    {
                        var subIntroducerBankDetails = new CE_BankDetails
                        {
                            EntityType = "SubIntroducer",
                            EntityID = subIntroducerEntity.SubIntroducerID, // Use the correct ID from the created entity
                            BankName = subIntroducer.BankDetails.BankName,
                            BankBranchAddress = subIntroducer.BankDetails.BankBranchAddress,
                            ReceiversAddress = subIntroducer.BankDetails.ReceiversAddress,
                            AccountName = subIntroducer.BankDetails.AccountName,
                            AccountSortCode = subIntroducer.BankDetails.AccountSortCode,
                            AccountNumber = subIntroducer.BankDetails.AccountNumber,
                            IBAN = subIntroducer.BankDetails.IBAN,
                            SwiftCode = subIntroducer.BankDetails.SwiftCode
                        };
                        db.CE_BankDetails.Add(subIntroducerBankDetails);
                    }

                    // Add Company Tax Info if provided (move from AddSubEntityBankDetailsAndTaxInfo to fix EntityID=1 bug)
                    if (subIntroducer.CompanyTaxInfo != null && !string.IsNullOrEmpty(subIntroducer.CompanyTaxInfo.CompanyRegistration))
                    {
                        var subIntroducerCompanyTaxInfo = new CE_CompanyTaxInfo
                        {
                            EntityType = "SubIntroducer",
                            EntityID = subIntroducerEntity.SubIntroducerID, // Use the correct ID from the created entity
                            CompanyRegistration = subIntroducer.CompanyTaxInfo.CompanyRegistration,
                            VATNumber = subIntroducer.CompanyTaxInfo.VATNumber,
                            Notes = subIntroducer.CompanyTaxInfo.Notes
                        };
                        db.CE_CompanyTaxInfo.Add(subIntroducerCompanyTaxInfo);
                    }
                }
            }
        }

        #endregion

        #region EditSector

        [Authorize(Roles = "Controls")]
        public ActionResult Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Logic.Logger.Log("Sector ID is null or empty - redirecting to dashboard");
                TempData["ToastMessage"] = "Sector ID is required.";
                TempData["ToastType"] = "error";
                return RedirectToAction("Dashboard", "Sector");
            }

            try
            {
                if (!int.TryParse(id, out int sectorId))
                {
                    Logic.Logger.Log($"Failed to parse id '{id}' to int");
                    TempData["ToastMessage"] = "Invalid sector ID.";
                    TempData["ToastType"] = "error";
                    return RedirectToAction("Dashboard", "Sector");
                }

                Logic.Logger.Log($"Successfully parsed id '{id}' to int: {sectorId}");

                var sector = db.CE_Sector
                    .Include(s => s.CE_BrokerageStaff)
                    .Include(s => s.CE_SubBrokerages.Select(sb => sb.CE_SubBrokerageCommissionAndPayments))
                    .Include(s => s.CE_SubReferrals.Select(sr => sr.CE_SubReferralCommissionAndPayments))
                    .Include(s => s.CE_SubIntroducers.Select(si => si.CE_SubIntroducerCommissionAndPayments))
                    .Include(s => s.SectorSuppliers)
                    .FirstOrDefault(s => s.SectorID == sectorId);

                if (sector == null)
                {
                    Logic.Logger.Log($"Sector with ID {sectorId} not found in database");
                    TempData["ToastMessage"] = "Sector not found.";
                    TempData["ToastType"] = "error";
                    return RedirectToAction("Dashboard", "Sector");
                }

                Logic.Logger.Log($"Found sector: {sector.Name} (ID: {sector.SectorID})");

                // Get Bank Details
                var bankDetails = db.CE_BankDetails
                    .Where(b => b.EntityType == "Sector" && b.EntityID == sector.SectorID)
                    .FirstOrDefault();

                // Get Company Tax Info
                var companyTaxInfo = db.CE_CompanyTaxInfo
                    .Where(c => c.EntityType == "Sector" && c.EntityID == sector.SectorID)
                    .FirstOrDefault();

                // Get Commission Records
                var brokerageCommissions = db.CE_BrokerageCommissionAndPayment
                    .Where(c => c.SectorID == sector.SectorID)
                    .ToList();

                var closerCommissions = db.CE_CloserCommissionAndPayment
                    .Where(c => c.SectorID == sector.SectorID)
                    .ToList();

                var introducerCommissions = db.CE_IntroducerCommissionAndPayment
                    .Where(c => c.SectorID == sector.SectorID)
                    .ToList();

                var referralPartnerCommissions = db.CE_ReferralPartnerCommissionAndPayment
                    .Where(c => c.SectorID == sector.SectorID)
                    .ToList();

                var leadGeneratorCommissions = db.CE_LeadGeneratorCommissionAndPayment
                    .Where(c => c.SectorID == sector.SectorID)
                    .ToList();

                // Build the complete EditSectorViewModel
                var model = BuildEditSectorViewModel(sector, bankDetails, companyTaxInfo,
                    brokerageCommissions, closerCommissions, introducerCommissions,
                    referralPartnerCommissions, leadGeneratorCommissions, db);

                Logic.Logger.Log($"Successfully built EditSectorViewModel for sector {sector.SectorID}");
                return View(model);
            }
            catch (Exception ex)
            {
                Logic.Logger.Log($"Failed to load sector for edit: {ex.Message}");
                TempData["ToastMessage"] = "Failed to load sector details.";
                TempData["ToastType"] = "error";
                return RedirectToAction("Dashboard", "Sector");
            }
        }

        private EditSectorViewModel BuildEditSectorViewModel(CE_Sector sector,
            CE_BankDetails bankDetails, CE_CompanyTaxInfo companyTaxInfo,
            List<CE_BrokerageCommissionAndPayment> brokerageCommissions,
            List<CE_CloserCommissionAndPayment> closerCommissions,
            List<CE_IntroducerCommissionAndPayment> introducerCommissions,
            List<CE_ReferralPartnerCommissionAndPayment> referralPartnerCommissions,
            List<CE_LeadGeneratorCommissionAndPayment> leadGeneratorCommissions,
            ApplicationDBContext db)
        {
            var model = new EditSectorViewModel
            {
                SectorId = sector.SectorID.ToString(),
                SectorType = sector.SectorType,
                Name = sector.Name,
                OfgemID = sector.OfgemID,
                Active = sector.Active,
                StartDate = sector.StartDate?.ToString("yyyy-MM-dd") ?? "",
                EndDate = sector.EndDate?.ToString("yyyy-MM-dd") ?? "",
                Mobile = sector.Mobile ?? "",
                Email = sector.Email ?? "",
                Landline = sector.Landline ?? "",
                Department = sector.Department ?? ""
            };

            // Set Bank Details
            if (bankDetails != null)
            {
                model.BankDetails = new BankDetailsViewModel
                {
                    BankName = bankDetails.BankName ?? "",
                    BankBranchAddress = bankDetails.BankBranchAddress ?? "",
                    ReceiversAddress = bankDetails.ReceiversAddress ?? "",
                    AccountName = bankDetails.AccountName ?? "",
                    AccountSortCode = bankDetails.AccountSortCode ?? "",
                    AccountNumber = bankDetails.AccountNumber ?? "",
                    IBAN = bankDetails.IBAN ?? "",
                    SwiftCode = bankDetails.SwiftCode ?? ""
                };
            }

            // Set Company Tax Info
            if (companyTaxInfo != null)
            {
                model.CompanyTaxInfo = new CompanyTaxInfoViewModel
                {
                    CompanyRegistration = companyTaxInfo.CompanyRegistration ?? "",
                    VATNumber = companyTaxInfo.VATNumber ?? "",
                    Notes = companyTaxInfo.Notes ?? ""
                };
            }

            // Set Commission Records
            if (brokerageCommissions.Any())
            {
                model.BrokerageCommissions = brokerageCommissions.Select(c => new BrokerageCommissionAndPaymentViewModel
                {
                    Id = c.CommissionID,
                    Commission = c.Commission,
                    StartDate = c.StartDate.ToString("yyyy-MM-dd"),
                    EndDate = c.EndDate?.ToString("yyyy-MM-dd") ?? "",
                    PaymentTerms = c.PaymentTerms ?? "",
                    CommissionType = c.CommissionType ?? ""
                }).ToList();
            }

            if (closerCommissions.Any())
            {
                model.CloserCommissions = closerCommissions.Select(c => new CloserCommissionAndPaymentViewModel
                {
                    Id = c.CommissionID,
                    Commission = c.Commission,
                    StartDate = c.StartDate.ToString("yyyy-MM-dd"),
                    EndDate = c.EndDate?.ToString("yyyy-MM-dd") ?? "",
                    PaymentTerms = c.PaymentTerms ?? "",
                    CommissionType = c.CommissionType ?? ""
                }).ToList();
            }

            if (introducerCommissions.Any())
            {
                model.IntroducerCommissions = introducerCommissions.Select(c => new IntroducerCommissionAndPaymentViewModel
                {
                    Id = c.CommissionID,
                    Commission = c.CommissionPercent,
                    StartDate = c.StartDate.ToString("yyyy-MM-dd"),
                    EndDate = c.EndDate?.ToString("yyyy-MM-dd") ?? "",
                    PaymentTerms = c.PaymentTerms ?? "",
                    CommissionType = c.CommissionType ?? ""
                }).ToList();
            }

            if (referralPartnerCommissions.Any())
            {
                model.ReferralPartnerCommissions = referralPartnerCommissions.Select(c => new ReferralPartnerCommissionAndPaymentViewModel
                {
                    Id = c.ReferralCommPayID,
                    ReferralPartnerCommission = c.ReferralPartnerCommission,
                    ReferralPartnerStartDate = c.ReferralPartnerStartDate?.ToString("yyyy-MM-dd") ?? "",
                    ReferralPartnerEndDate = c.ReferralPartnerEndDate?.ToString("yyyy-MM-dd") ?? "",
                    PaymentTerms = c.PaymentTerms ?? "",
                    CommissionType = c.CommissionType ?? ""
                }).ToList();
            }

            if (leadGeneratorCommissions.Any())
            {
                model.LeadGeneratorCommissions = leadGeneratorCommissions.Select(c => new LeadGeneratorCommissionAndPaymentViewModel
                {
                    Id = c.LeadGenCommPayID,
                    LeadGeneratorCommission = c.LeadGeneratorCommissionPercent,
                    LeadGeneratorStartDate = c.LeadGeneratorStartDate?.ToString("yyyy-MM-dd") ?? "",
                    LeadGeneratorEndDate = c.LeadGeneratorEndDate?.ToString("yyyy-MM-dd") ?? "",
                    CloserCommission = c.CloserCommissionPercent,
                    CloserStartDate = c.CloserStartDate?.ToString("yyyy-MM-dd") ?? "",
                    CloserEndDate = c.CloserEndDate?.ToString("yyyy-MM-dd") ?? "",
                    PaymentTerms = c.PaymentTerms ?? "",
                    CommissionType = c.CommissionType ?? ""
                }).ToList();
            }

            // Set Staff and Sub-sections
            if (sector.CE_BrokerageStaff.Any())
            {
                model.BrokerageStaff = sector.CE_BrokerageStaff.Select(s => new BrokerageStaffViewModel
                {
                    BrokerageStaffName = s.BrokerageStaffName ?? "",
                    Email = s.Email ?? "",
                    Mobile = s.Mobile ?? "",
                    Landline = s.Landline ?? "",
                    StartDate = s.StartDate?.ToString("yyyy-MM-dd") ?? "",
                    EndDate = s.EndDate?.ToString("yyyy-MM-dd") ?? "",
                    Active = s.Active
                }).ToList();
            }

            if (sector.CE_SubBrokerages.Any())
            {
                model.SubBrokerages = sector.CE_SubBrokerages.Select(s => new SubBrokerageViewModel
                {
                    SubBrokerageName = s.SubBrokerageName ?? "",
                    OfgemID = s.OfgemID ?? "",
                    Email = s.Email ?? "",
                    Landline = s.Landline ?? "",
                    Mobile = s.Mobile ?? "",
                    StartDate = s.StartDate?.ToString("yyyy-MM-dd") ?? "",
                    EndDate = s.EndDate?.ToString("yyyy-MM-dd") ?? "",
                    Active = s.Active,

                    BankDetails = db.CE_BankDetails
                        .Where(b => b.EntityType == "SubBrokerage" && b.EntityID == s.SubBrokerageID)
                        .Select(b => new BankDetailsViewModel
                        {
                            BankName = b.BankName ?? "",
                            BankBranchAddress = b.BankBranchAddress ?? "",
                            ReceiversAddress = b.ReceiversAddress ?? "",
                            AccountName = b.AccountName ?? "",
                            AccountSortCode = b.AccountSortCode ?? "",
                            AccountNumber = b.AccountNumber ?? "",
                            IBAN = b.IBAN ?? "",
                            SwiftCode = b.SwiftCode ?? ""
                        }).FirstOrDefault() ?? new BankDetailsViewModel(),

                    // Company Tax Info - CORRECTED: Query by EntityType and EntityID
                    CompanyTaxInfo = db.CE_CompanyTaxInfo
                        .Where(c => c.EntityType == "SubBrokerage" && c.EntityID == s.SubBrokerageID)
                        .Select(c => new CompanyTaxInfoViewModel
                        {
                            CompanyRegistration = c.CompanyRegistration ?? "",
                            VATNumber = c.VATNumber ?? "",
                            Notes = c.Notes ?? ""
                        }).FirstOrDefault() ?? new CompanyTaxInfoViewModel(),


                    Commissions = s.CE_SubBrokerageCommissionAndPayments?.Select(c => new SubBrokerageCommissionAndPaymentViewModel
                    {
                        SubBrokerageCommission = c.SubBrokerageCommissionPercent,
                        BrokerageCommission = c.BrokerageCommissionPercent,
                        SubBrokerageStartDate = c.SubBrokerageStartDate?.ToString("yyyy-MM-dd") ?? "",
                        SubBrokerageEndDate = c.SubBrokerageEndDate?.ToString("yyyy-MM-dd") ?? "",
                        BrokerageStartDate = c.BrokerageStartDate?.ToString("yyyy-MM-dd") ?? "",
                        BrokerageEndDate = c.BrokerageEndDate?.ToString("yyyy-MM-dd") ?? "",
                        PaymentTerms = c.PaymentTerms ?? "",
                        CommissionType = c.CommissionType ?? ""
                    }).ToList() ?? new List<SubBrokerageCommissionAndPaymentViewModel>()
                }).ToList();
            }

            if (sector.CE_SubReferrals.Any())
            {
                model.SubReferrals = sector.CE_SubReferrals.Select(s => new SubReferralViewModel
                {
                    SubReferralPartnerName = s.SubReferralPartnerName ?? "",
                    Email = s.SubReferralPartnerEmail ?? "",
                    Landline = s.SubReferralPartnerLandline ?? "",
                    Mobile = s.SubReferralPartnerMobile ?? "",
                    StartDate = s.StartDate?.ToString("yyyy-MM-dd") ?? "",
                    EndDate = s.EndDate?.ToString("yyyy-MM-dd") ?? "",
                    Active = s.Active,

                    // Bank Details - CORRECTED: Query by EntityType and EntityID
                    BankDetails = db.CE_BankDetails
                        .Where(b => b.EntityType == "SubReferral" && b.EntityID == s.SubReferralID)
                        .Select(b => new BankDetailsViewModel
                        {
                            BankName = b.BankName ?? "",
                            BankBranchAddress = b.BankBranchAddress ?? "",
                            ReceiversAddress = b.ReceiversAddress ?? "",
                            AccountName = b.AccountName ?? "",
                            AccountSortCode = b.AccountSortCode ?? "",
                            AccountNumber = b.AccountNumber ?? "",
                            IBAN = b.IBAN ?? "",
                            SwiftCode = b.SwiftCode ?? ""
                        }).FirstOrDefault() ?? new BankDetailsViewModel(),

                    CompanyTaxInfo = db.CE_CompanyTaxInfo
                        .Where(c => c.EntityType == "SubReferral" && c.EntityID == s.SubReferralID)
                        .Select(c => new CompanyTaxInfoViewModel
                        {
                            CompanyRegistration = c.CompanyRegistration ?? "",
                            VATNumber = c.VATNumber ?? "",
                            Notes = c.Notes ?? ""
                        }).FirstOrDefault() ?? new CompanyTaxInfoViewModel(),

                    Commissions = s.CE_SubReferralCommissionAndPayments?.Select(c => new SubReferralCommissionAndPaymentViewModel
                    {
                        SubReferralCommission = c.SubIntroducerCommission, // DB.SubIntroducerCommission -> ViewModel.SubReferralCommission
                        SubReferralStartDate = c.SubIntroducerStartDate?.ToString("yyyy-MM-dd") ?? "",
                        SubReferralEndDate = c.SubIntroducerEndDate?.ToString("yyyy-MM-dd") ?? "",
                        ReferralPartnerCommission = c.IntroducerCommission, // DB.IntroducerCommission -> ViewModel.ReferralPartnerCommission
                        ReferralPartnerStartDate = c.IntroducerStartDate?.ToString("yyyy-MM-dd") ?? "",
                        ReferralPartnerEndDate = c.IntroducerEndDate?.ToString("yyyy-MM-dd") ?? "",
                        PaymentTerms = c.PaymentTerms ?? "",
                        CommissionType = c.CommissionType ?? ""
                    }).ToList() ?? new List<SubReferralCommissionAndPaymentViewModel>()
                }).ToList();
            }

            if (sector.CE_SubIntroducers.Any())
            {
                model.SubIntroducers = sector.CE_SubIntroducers.Select(s => new SubIntroducerViewModel
                {
                    SubIntroducerName = s.SubIntroducerName ?? "",
                    OfgemID = s.OfgemID ?? "",
                    Email = s.SubIntroducerEmail ?? "",
                    Landline = s.SubIntroducerLandline ?? "",
                    Mobile = s.SubIntroducerMobile ?? "",
                    StartDate = s.StartDate?.ToString("yyyy-MM-dd") ?? "",
                    EndDate = s.EndDate?.ToString("yyyy-MM-dd") ?? "",
                    Active = s.Active,

                    // Bank Details - CORRECTED: Query by EntityType and EntityID
                    BankDetails = db.CE_BankDetails
                        .Where(b => b.EntityType == "SubIntroducer" && b.EntityID == s.SubIntroducerID)
                        .Select(b => new BankDetailsViewModel
                        {
                            BankName = b.BankName ?? "",
                            BankBranchAddress = b.BankBranchAddress ?? "",
                            ReceiversAddress = b.ReceiversAddress ?? "",
                            AccountName = b.AccountName ?? "",
                            AccountSortCode = b.AccountSortCode ?? "",
                            AccountNumber = b.AccountNumber ?? "",
                            IBAN = b.IBAN ?? "",
                            SwiftCode = b.SwiftCode ?? ""
                        }).FirstOrDefault() ?? new BankDetailsViewModel(),

                    // Company Tax Info - CORRECTED: Query by EntityType and EntityID
                    CompanyTaxInfo = db.CE_CompanyTaxInfo
                        .Where(c => c.EntityType == "SubIntroducer" && c.EntityID == s.SubIntroducerID)
                        .Select(c => new CompanyTaxInfoViewModel
                        {
                            CompanyRegistration = c.CompanyRegistration ?? "",
                            VATNumber = c.VATNumber ?? "",
                            Notes = c.Notes ?? ""
                        }).FirstOrDefault() ?? new CompanyTaxInfoViewModel(),

                    // Commissions - CORRECTED: Fixed field names to match DB model and ViewModel
                    Commissions = s.CE_SubIntroducerCommissionAndPayments?.Select(c => new SubIntroducerCommissionAndPaymentViewModel
                    {
                        SubIntroducerCommission = c.SubIntroducerCommission,
                        SubIntroducerStartDate = c.SubIntroducerCommissionStartDate?.ToString("yyyy-MM-dd") ?? "",
                        SubIntroducerEndDate = c.SubIntroducerCommissionEndDate?.ToString("yyyy-MM-dd") ?? "",
                        IntroducerCommission = c.IntroducerCommission,
                        IntroducerStartDate = c.IntroducerCommissionStartDate?.ToString("yyyy-MM-dd") ?? "",
                        IntroducerEndDate = c.IntroducerCommissionEndDate?.ToString("yyyy-MM-dd") ?? "",
                        PaymentTerms = c.PaymentTerms ?? "",
                        CommissionType = c.CommissionType ?? ""
                    }).ToList() ?? new List<SubIntroducerCommissionAndPaymentViewModel>()
                }).ToList();
            }

            // Set Sector Suppliers
            if (sector.SectorSuppliers != null && sector.SectorSuppliers.Any())
            {
                model.SectorSuppliers = sector.SectorSuppliers.Select(ss => ss.SupplierId).ToList();
            }

            return model;
        }

        [HttpPost]
        [Authorize(Roles = "Controls")]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> Edit(EditSectorViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                                 .Where(x => x.Value.Errors.Count > 0)
                                 .SelectMany(kvp => kvp.Value.Errors)
                                 .Select(e => e.ErrorMessage)
                                 .ToList();

                    return Json(new
                    {
                        success = false,
                        message = "Validation failed. Please check your input.",
                        errors = errors
                    }, JsonRequestBehavior.AllowGet);
                }

                // Additional validation for Brokerage and Introducers
                if ((model.SectorType == "Brokerage" || model.SectorType == "Introducer") &&
                    string.IsNullOrWhiteSpace(model.OfgemID))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Ofgem ID is required for Brokerage and Introducer sectors.",
                        errors = new[] { "Ofgem ID is required for Brokerage and Introducer sectors." }
                    }, JsonRequestBehavior.AllowGet);
                }

                if (!int.TryParse(model.SectorId, out int sectorId))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid sector ID.",
                        errors = new[] { "Invalid sector ID." }
                    }, JsonRequestBehavior.AllowGet);
                }

                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        // Step 1: Update the main sector
                        var sector = await db.CE_Sector.FindAsync(sectorId);
                        if (sector == null)
                        {
                            return Json(new
                            {
                                success = false,
                                message = "Sector not found.",
                                errors = new[] { "Sector not found." }
                            }, JsonRequestBehavior.AllowGet);
                        }

                        sector.Name = model.Name;
                        sector.Active = model.Active;
                        sector.StartDate = !string.IsNullOrEmpty(model.StartDate) ? DateTime.Parse(model.StartDate) : (DateTime?)null;
                        sector.EndDate = !string.IsNullOrEmpty(model.EndDate) ? DateTime.Parse(model.EndDate) : (DateTime?)null;
                        sector.Email = model.Email;
                        sector.Mobile = model.Mobile;
                        sector.OfgemID = model.OfgemID;
                        sector.SectorType = model.SectorType;
                        sector.Landline = model.Landline;
                        sector.Department = model.Department;

                        // Step 2: Update Bank Details
                        var existingBankDetails = await db.CE_BankDetails
                            .Where(b => b.EntityType == "Sector" && b.EntityID == sectorId)
                            .FirstOrDefaultAsync();

                        if (existingBankDetails != null)
                        {
                            // Update existing
                            existingBankDetails.BankName = model.BankDetails.BankName;
                            existingBankDetails.BankBranchAddress = model.BankDetails.BankBranchAddress;
                            existingBankDetails.ReceiversAddress = model.BankDetails.ReceiversAddress;
                            existingBankDetails.AccountName = model.BankDetails.AccountName;
                            existingBankDetails.AccountSortCode = model.BankDetails.AccountSortCode;
                            existingBankDetails.AccountNumber = model.BankDetails.AccountNumber;
                            existingBankDetails.IBAN = model.BankDetails.IBAN;
                            existingBankDetails.SwiftCode = model.BankDetails.SwiftCode;
                        }
                        else if (!string.IsNullOrEmpty(model.BankDetails.BankName))
                        {
                            // Create new
                            var bankDetails = new CE_BankDetails
                            {
                                EntityType = "Sector",
                                EntityID = sectorId,
                                BankName = model.BankDetails.BankName,
                                BankBranchAddress = model.BankDetails.BankBranchAddress,
                                ReceiversAddress = model.BankDetails.ReceiversAddress,
                                AccountName = model.BankDetails.AccountName,
                                AccountSortCode = model.BankDetails.AccountSortCode,
                                AccountNumber = model.BankDetails.AccountNumber,
                                IBAN = model.BankDetails.IBAN,
                                SwiftCode = model.BankDetails.SwiftCode
                            };
                            db.CE_BankDetails.Add(bankDetails);
                        }

                        // Step 3: Update Company Tax Info
                        var existingCompanyTaxInfo = await db.CE_CompanyTaxInfo
                            .Where(c => c.EntityType == "Sector" && c.EntityID == sectorId)
                            .FirstOrDefaultAsync();

                        if (existingCompanyTaxInfo != null)
                        {
                            // Update existing
                            existingCompanyTaxInfo.CompanyRegistration = model.CompanyTaxInfo.CompanyRegistration;
                            existingCompanyTaxInfo.VATNumber = model.CompanyTaxInfo.VATNumber;
                            existingCompanyTaxInfo.Notes = model.CompanyTaxInfo.Notes;
                        }
                        else if (!string.IsNullOrEmpty(model.CompanyTaxInfo.CompanyRegistration))
                        {
                            // Create new
                            var companyTaxInfo = new CE_CompanyTaxInfo
                            {
                                EntityType = "Sector",
                                EntityID = sectorId,
                                CompanyRegistration = model.CompanyTaxInfo.CompanyRegistration,
                                VATNumber = model.CompanyTaxInfo.VATNumber,
                                Notes = model.CompanyTaxInfo.Notes
                            };
                            db.CE_CompanyTaxInfo.Add(companyTaxInfo);
                        }

                        // Step 4: Update Commission Records
                        await UpdateCommissionRecords(sectorId, model);


                        // Step 5: Update Staff and Sub-sections
                        await UpdateStaffAndSubsections(sectorId, model);

                        // Step 6: Handle Sector Suppliers (for Brokerage sector type)
                        await HandleSectorSuppliers(sectorId, model.SectorSuppliers);

                        await db.SaveChangesAsync();
                        transaction.Commit();

                        // Return JSON for immediate toast notification
                        return Json(new
                        {
                            success = true,
                            message = "Sector updated successfully!",
                            redirectUrl = Url.Action("Dashboard", "Sector")
                        }, JsonRequestBehavior.AllowGet);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Logic.Logger.Log($"Sector update transaction failed: {ex.Message}");

                        // Return JSON error for immediate toast notification
                        return Json(new
                        {
                            success = false,
                            message = $"Failed to update sector: {ex.Message}",
                            errors = new[] { ex.Message }
                        }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            catch (Exception ex)
            {
                Logic.Logger.Log($"Sector update failed!! {ex.Message}");

                // Return JSON error for immediate toast notification
                return Json(new
                {
                    success = false,
                    message = $"An error occurred: {ex.Message}",
                    errors = new[] { ex.Message }
                }, JsonRequestBehavior.AllowGet);
            }
        }



        #endregion

        #region Helper Methods for Edit

        private async Task UpdateCommissionRecords(int sectorId, EditSectorViewModel model)
        {
            // Update Brokerage Commissions - Only for Brokerage sector type
            if (model.SectorType == "Brokerage" && model.BrokerageCommissions != null && model.BrokerageCommissions.Any(c => !string.IsNullOrEmpty(c.Commission.ToString())))
            {
                var existingBrokerageCommissions = await db.CE_BrokerageCommissionAndPayment
                    .Where(c => c.SectorID == sectorId)
                    .ToListAsync();

                // Process each commission from the model
                foreach (var commission in model.BrokerageCommissions.Where(c => c != null && !string.IsNullOrEmpty(c.Commission.ToString())))
                {
                    if (commission.Id.HasValue)
                    {
                        // Update existing commission
                        var existingCommission = existingBrokerageCommissions
                            .FirstOrDefault(ec => ec.CommissionID == commission.Id.Value);

                        if (existingCommission != null)
                        {
                            existingCommission.Commission = commission.Commission;
                            existingCommission.StartDate = (DateTime)(!string.IsNullOrEmpty(commission.StartDate) ? DateTime.Parse(commission.StartDate) : (DateTime?)null);
                            existingCommission.EndDate = !string.IsNullOrEmpty(commission.EndDate) ? DateTime.Parse(commission.EndDate) : (DateTime?)null;
                            existingCommission.PaymentTerms = commission.PaymentTerms ?? "";
                            existingCommission.CommissionType = commission.CommissionType ?? "";

                            existingBrokerageCommissions.Remove(existingCommission);
                        }
                    }
                    else
                    {
                        // Create new commission
                        var brokerageCommission = new CE_BrokerageCommissionAndPayment
                        {
                            SectorID = sectorId,
                            Commission = commission.Commission,
                            StartDate = (DateTime)(!string.IsNullOrEmpty(commission.StartDate) ? DateTime.Parse(commission.StartDate) : (DateTime?)null),
                            EndDate = !string.IsNullOrEmpty(commission.EndDate) ? DateTime.Parse(commission.EndDate) : (DateTime?)null,
                            PaymentTerms = commission.PaymentTerms ?? "",
                            CommissionType = commission.CommissionType ?? ""
                        };
                        db.CE_BrokerageCommissionAndPayment.Add(brokerageCommission);
                    }
                }

                // Remove any remaining existing commissions that weren't updated
                if (existingBrokerageCommissions.Any())
                {
                    db.CE_BrokerageCommissionAndPayment.RemoveRange(existingBrokerageCommissions);
                }
            }

            // Update Closer Commissions - Only for Closer sector type
            if (model.SectorType == "Closer" && model.CloserCommissions != null && model.CloserCommissions.Any())
            {
                var existingCloserCommissions = await db.CE_CloserCommissionAndPayment
                    .Where(c => c.SectorID == sectorId)
                    .ToListAsync();

                foreach (var commission in model.CloserCommissions.Where(c => c != null && !string.IsNullOrEmpty(c.Commission.ToString())))
                {
                    if (commission.Id.HasValue)
                    {
                        // Update existing commission
                        var existingCommission = existingCloserCommissions
                            .FirstOrDefault(ec => ec.CommissionID == commission.Id.Value);

                        if (existingCommission != null)
                        {
                            existingCommission.Commission = commission.Commission;
                            existingCommission.StartDate = (DateTime)(!string.IsNullOrEmpty(commission.StartDate) ? DateTime.Parse(commission.StartDate) : (DateTime?)null);
                            existingCommission.EndDate = !string.IsNullOrEmpty(commission.EndDate) ? DateTime.Parse(commission.EndDate) : (DateTime?)null;
                            existingCommission.PaymentTerms = commission.PaymentTerms ?? "";
                            existingCommission.CommissionType = commission.CommissionType ?? "";

                            existingCloserCommissions.Remove(existingCommission);
                        }
                    }
                    else
                    {
                        // Create new commission
                        var closerCommission = new CE_CloserCommissionAndPayment
                        {
                            SectorID = sectorId,
                            Commission = commission.Commission,
                            StartDate = (DateTime)(!string.IsNullOrEmpty(commission.StartDate) ? DateTime.Parse(commission.StartDate) : (DateTime?)null),
                            EndDate = !string.IsNullOrEmpty(commission.EndDate) ? DateTime.Parse(commission.EndDate) : (DateTime?)null,
                            PaymentTerms = commission.PaymentTerms ?? "",
                            CommissionType = commission.CommissionType ?? ""
                        };
                        db.CE_CloserCommissionAndPayment.Add(closerCommission);
                    }
                }

                // Remove any remaining existing commissions that weren't updated
                if (existingCloserCommissions.Any())
                {
                    db.CE_CloserCommissionAndPayment.RemoveRange(existingCloserCommissions);
                }
            }

            // Update Introducer Commissions - Only for Introducer sector type
            if (model.SectorType == "Introducer" && model.IntroducerCommissions != null && model.IntroducerCommissions.Any())
            {
                var existingIntroducerCommissions = await db.CE_IntroducerCommissionAndPayment
                    .Where(c => c.SectorID == sectorId)
                    .ToListAsync();

                foreach (var commission in model.IntroducerCommissions.Where(c => c != null && !string.IsNullOrEmpty(c.Commission.ToString())))
                {
                    if (commission.Id.HasValue)
                    {
                        // Update existing commission
                        var existingCommission = existingIntroducerCommissions
                            .FirstOrDefault(ec => ec.CommissionID == commission.Id.Value);

                        if (existingCommission != null)
                        {
                            existingCommission.CommissionPercent = commission.Commission;
                            existingCommission.StartDate = (DateTime)(!string.IsNullOrEmpty(commission.StartDate) ? DateTime.Parse(commission.StartDate) : (DateTime?)null);
                            existingCommission.EndDate = !string.IsNullOrEmpty(commission.EndDate) ? DateTime.Parse(commission.EndDate) : (DateTime?)null;
                            existingCommission.PaymentTerms = commission.PaymentTerms ?? "";
                            existingCommission.CommissionType = commission.CommissionType ?? "";

                            existingIntroducerCommissions.Remove(existingCommission);
                        }
                    }
                    else
                    {
                        // Create new commission
                        var introducerCommission = new CE_IntroducerCommissionAndPayment
                        {
                            SectorID = sectorId,
                            CommissionPercent = commission.Commission,
                            StartDate = (DateTime)(!string.IsNullOrEmpty(commission.StartDate) ? DateTime.Parse(commission.StartDate) : (DateTime?)null),
                            EndDate = !string.IsNullOrEmpty(commission.EndDate) ? DateTime.Parse(commission.EndDate) : (DateTime?)null,
                            PaymentTerms = commission.PaymentTerms ?? "",
                            CommissionType = commission.CommissionType ?? ""
                        };
                        db.CE_IntroducerCommissionAndPayment.Add(introducerCommission);
                    }
                }

                // Remove any remaining existing commissions that weren't updated
                if (existingIntroducerCommissions.Any())
                {
                    db.CE_IntroducerCommissionAndPayment.RemoveRange(existingIntroducerCommissions);
                }
            }

            // Update Referral Partner Commissions - Only for Referral Partner sector type
            if (model.SectorType == "Referral Partner" && model.ReferralPartnerCommissions != null && model.ReferralPartnerCommissions.Any())
            {
                var existingReferralPartnerCommissions = await db.CE_ReferralPartnerCommissionAndPayment
                    .Where(c => c.SectorID == sectorId)
                    .ToListAsync();

                foreach (var commission in model.ReferralPartnerCommissions.Where(c => c != null && !string.IsNullOrEmpty(c.ReferralPartnerCommission.ToString())))
                {
                    if (commission.Id.HasValue)
                    {
                        // Update existing commission
                        var existingCommission = existingReferralPartnerCommissions
                            .FirstOrDefault(ec => ec.ReferralCommPayID == commission.Id.Value);

                        if (existingCommission != null)
                        {
                            existingCommission.ReferralPartnerCommission = commission.ReferralPartnerCommission;
                            existingCommission.ReferralPartnerStartDate = !string.IsNullOrEmpty(commission.ReferralPartnerStartDate) ? DateTime.Parse(commission.ReferralPartnerStartDate) : (DateTime?)null;
                            existingCommission.ReferralPartnerEndDate = !string.IsNullOrEmpty(commission.ReferralPartnerEndDate) ? DateTime.Parse(commission.ReferralPartnerEndDate) : (DateTime?)null;
                            existingCommission.PaymentTerms = commission.PaymentTerms ?? "";
                            existingCommission.CommissionType = commission.CommissionType ?? "";

                            existingReferralPartnerCommissions.Remove(existingCommission);
                        }
                    }
                    else
                    {
                        // Create new commission
                        var referralPartnerCommission = new CE_ReferralPartnerCommissionAndPayment
                        {
                            SectorID = sectorId,
                            ReferralPartnerCommission = commission.ReferralPartnerCommission,
                            ReferralPartnerStartDate = !string.IsNullOrEmpty(commission.ReferralPartnerStartDate) ? DateTime.Parse(commission.ReferralPartnerStartDate) : (DateTime?)null,
                            ReferralPartnerEndDate = !string.IsNullOrEmpty(commission.ReferralPartnerEndDate) ? DateTime.Parse(commission.ReferralPartnerEndDate) : (DateTime?)null,
                            PaymentTerms = commission.PaymentTerms ?? "",
                            CommissionType = commission.CommissionType ?? ""
                        };
                        db.CE_ReferralPartnerCommissionAndPayment.Add(referralPartnerCommission);
                    }
                }

                // Remove any remaining existing commissions that weren't updated
                if (existingReferralPartnerCommissions.Any())
                {
                    db.CE_ReferralPartnerCommissionAndPayment.RemoveRange(existingReferralPartnerCommissions);
                }
            }

            // Update Lead Generator Commissions - Only for Leads Generator sector type
            if (model.SectorType == "Leads Generator" && model.LeadGeneratorCommissions != null && model.LeadGeneratorCommissions.Any())
            {
                var existingLeadGeneratorCommissions = await db.CE_LeadGeneratorCommissionAndPayment
                    .Where(c => c.SectorID == sectorId)
                    .ToListAsync();

                foreach (var commission in model.LeadGeneratorCommissions.Where(c => c != null && !string.IsNullOrEmpty(c.LeadGeneratorCommission.ToString())))
                {
                    if (commission.Id.HasValue)
                    {
                        // Update existing commission
                        var existingCommission = existingLeadGeneratorCommissions
                            .FirstOrDefault(ec => ec.LeadGenCommPayID == commission.Id.Value);

                        if (existingCommission != null)
                        {
                            existingCommission.LeadGeneratorCommissionPercent = commission.LeadGeneratorCommission;
                            existingCommission.LeadGeneratorStartDate = !string.IsNullOrEmpty(commission.LeadGeneratorStartDate) ? DateTime.Parse(commission.LeadGeneratorStartDate) : (DateTime?)null;
                            existingCommission.LeadGeneratorEndDate = !string.IsNullOrEmpty(commission.LeadGeneratorEndDate) ? DateTime.Parse(commission.LeadGeneratorEndDate) : (DateTime?)null;
                            existingCommission.PaymentTerms = commission.PaymentTerms ?? "";
                            existingCommission.CommissionType = commission.CommissionType ?? "";
                            existingCommission.CloserCommissionPercent = commission.CloserCommission;
                            existingCommission.CloserStartDate = !string.IsNullOrEmpty(commission.CloserStartDate) ? DateTime.Parse(commission.CloserStartDate) : (DateTime?)null;
                            existingCommission.CloserEndDate = !string.IsNullOrEmpty(commission.CloserEndDate) ? DateTime.Parse(commission.CloserEndDate) : (DateTime?)null;

                            existingLeadGeneratorCommissions.Remove(existingCommission);
                        }
                    }
                    else
                    {
                        // Create new commission
                        var leadGeneratorCommission = new CE_LeadGeneratorCommissionAndPayment
                        {
                            SectorID = sectorId,
                            LeadGeneratorCommissionPercent = commission.LeadGeneratorCommission,
                            LeadGeneratorStartDate = !string.IsNullOrEmpty(commission.LeadGeneratorStartDate) ? DateTime.Parse(commission.LeadGeneratorStartDate) : (DateTime?)null,
                            LeadGeneratorEndDate = !string.IsNullOrEmpty(commission.LeadGeneratorEndDate) ? DateTime.Parse(commission.LeadGeneratorEndDate) : (DateTime?)null,
                            PaymentTerms = commission.PaymentTerms ?? "",
                            CommissionType = commission.CommissionType ?? "",
                            CloserCommissionPercent = commission.CloserCommission,
                            CloserStartDate = !string.IsNullOrEmpty(commission.CloserStartDate) ? DateTime.Parse(commission.CloserStartDate) : (DateTime?)null,
                            CloserEndDate = !string.IsNullOrEmpty(commission.CloserEndDate) ? DateTime.Parse(commission.CloserEndDate) : (DateTime?)null
                        };
                        db.CE_LeadGeneratorCommissionAndPayment.Add(leadGeneratorCommission);
                    }
                }

                // Remove any remaining existing commissions that weren't updated
                if (existingLeadGeneratorCommissions.Any())
                {
                    db.CE_LeadGeneratorCommissionAndPayment.RemoveRange(existingLeadGeneratorCommissions);
                }
            }
        }

        private async Task UpdateStaffAndSubsections(int sectorId, EditSectorViewModel model)
        {
            // Update Brokerage Staff
            if (model.BrokerageStaff != null && model.BrokerageStaff.Any())
            {
                var existingBrokerageStaff = await db.CE_BrokerageStaff
                    .Where(s => s.SectorID == sectorId)
                    .ToListAsync();
                db.CE_BrokerageStaff.RemoveRange(existingBrokerageStaff);

                foreach (var staff in model.BrokerageStaff.Where(s => s != null && !string.IsNullOrEmpty(s.BrokerageStaffName)))
                {
                    var brokerageStaff = new CE_BrokerageStaff
                    {
                        SectorID = sectorId,
                        BrokerageStaffName = staff.BrokerageStaffName,
                        Email = staff.Email ?? "",
                        Mobile = staff.Mobile ?? "",
                        Landline = staff.Landline ?? "",
                        StartDate = !string.IsNullOrEmpty(staff.StartDate) ? DateTime.Parse(staff.StartDate) : (DateTime?)null,
                        EndDate = !string.IsNullOrEmpty(staff.EndDate) ? DateTime.Parse(staff.EndDate) : (DateTime?)null,
                        Active = staff.Active
                    };
                    db.CE_BrokerageStaff.Add(brokerageStaff);
                }
            }

            // Update Sub Brokerages - Intelligent update strategy to preserve existing data
            if (model.SubBrokerages != null && model.SubBrokerages.Any(s => !string.IsNullOrEmpty(s.SubBrokerageName)))
            {
                var existingSubBrokerages = await db.CE_SubBrokerage
                    .Where(s => s.SectorID == sectorId)
                    .ToListAsync();

                var submittedNames = model.SubBrokerages
                    .Where(s => !string.IsNullOrEmpty(s.SubBrokerageName))
                    .Select(s => s.SubBrokerageName)
                    .ToList();

                // Remove SubBrokerages that are no longer present in the submission
                var subBrokeragesToRemove = existingSubBrokerages
                    .Where(existing => !submittedNames.Contains(existing.SubBrokerageName))
                    .ToList();

                if (subBrokeragesToRemove.Any())
                {
                    var removeIds = subBrokeragesToRemove.Select(s => s.SubBrokerageID).ToList();

                    // Remove related data for truly deleted SubBrokerages
                    var bankDetailsToRemove = await db.CE_BankDetails
                        .Where(b => b.EntityType == "SubBrokerage" && removeIds.Contains(b.EntityID))
                        .ToListAsync();
                    db.CE_BankDetails.RemoveRange(bankDetailsToRemove);

                    var companyTaxToRemove = await db.CE_CompanyTaxInfo
                        .Where(c => c.EntityType == "SubBrokerage" && removeIds.Contains(c.EntityID))
                        .ToListAsync();
                    db.CE_CompanyTaxInfo.RemoveRange(companyTaxToRemove);

                    var commissionsToRemove = await db.CE_SubBrokerageCommissionAndPayment
                        .Where(c => removeIds.Contains(c.SubBrokerageID))
                        .ToListAsync();
                    db.CE_SubBrokerageCommissionAndPayment.RemoveRange(commissionsToRemove);

                    db.CE_SubBrokerage.RemoveRange(subBrokeragesToRemove);
                }

                // Process each submitted SubBrokerage
                foreach (var subBrokerage in model.SubBrokerages.Where(s => s != null && !string.IsNullOrEmpty(s.SubBrokerageName)))
                {
                    var existingSubBrokerage = existingSubBrokerages
                        .FirstOrDefault(e => e.SubBrokerageName == subBrokerage.SubBrokerageName);

                    CE_SubBrokerage subBrokerageEntity;

                    if (existingSubBrokerage != null)
                    {
                        // Update existing SubBrokerage
                        existingSubBrokerage.OfgemID = subBrokerage.OfgemID ?? "";
                        existingSubBrokerage.Active = subBrokerage.Active;
                        existingSubBrokerage.StartDate = !string.IsNullOrEmpty(subBrokerage.StartDate) ? DateTime.Parse(subBrokerage.StartDate) : (DateTime?)null;
                        existingSubBrokerage.EndDate = !string.IsNullOrEmpty(subBrokerage.EndDate) ? DateTime.Parse(subBrokerage.EndDate) : (DateTime?)null;
                        existingSubBrokerage.Email = subBrokerage.Email ?? "";
                        existingSubBrokerage.Landline = subBrokerage.Landline ?? "";
                        existingSubBrokerage.Mobile = subBrokerage.Mobile ?? "";
                        subBrokerageEntity = existingSubBrokerage;

                        // Update commissions (replace all)
                        var existingCommissions = await db.CE_SubBrokerageCommissionAndPayment
                            .Where(c => c.SubBrokerageID == existingSubBrokerage.SubBrokerageID)
                            .ToListAsync();
                        db.CE_SubBrokerageCommissionAndPayment.RemoveRange(existingCommissions);
                    }
                    else
                    {
                        // Create new SubBrokerage
                        subBrokerageEntity = new CE_SubBrokerage
                        {
                            SectorID = sectorId,
                            SubBrokerageName = subBrokerage.SubBrokerageName,
                            OfgemID = subBrokerage.OfgemID ?? "",
                            Active = subBrokerage.Active,
                            StartDate = !string.IsNullOrEmpty(subBrokerage.StartDate) ? DateTime.Parse(subBrokerage.StartDate) : (DateTime?)null,
                            EndDate = !string.IsNullOrEmpty(subBrokerage.EndDate) ? DateTime.Parse(subBrokerage.EndDate) : (DateTime?)null,
                            Email = subBrokerage.Email ?? "",
                            Landline = subBrokerage.Landline ?? "",
                            Mobile = subBrokerage.Mobile ?? ""
                        };
                        db.CE_SubBrokerage.Add(subBrokerageEntity);
                        await db.SaveChangesAsync(); // Save to get ID for new entity
                    }

                    // Update/Create Bank Details if provided
                    if (subBrokerage.BankDetails != null && !string.IsNullOrEmpty(subBrokerage.BankDetails.BankName))
                    {
                        var existingBankDetails = await db.CE_BankDetails
                            .FirstOrDefaultAsync(b => b.EntityType == "SubBrokerage" && b.EntityID == subBrokerageEntity.SubBrokerageID);

                        if (existingBankDetails != null)
                        {
                            // Update existing bank details
                            existingBankDetails.BankName = subBrokerage.BankDetails.BankName;
                            existingBankDetails.BankBranchAddress = subBrokerage.BankDetails.BankBranchAddress;
                            existingBankDetails.ReceiversAddress = subBrokerage.BankDetails.ReceiversAddress;
                            existingBankDetails.AccountName = subBrokerage.BankDetails.AccountName;
                            existingBankDetails.AccountSortCode = subBrokerage.BankDetails.AccountSortCode;
                            existingBankDetails.AccountNumber = subBrokerage.BankDetails.AccountNumber;
                            existingBankDetails.IBAN = subBrokerage.BankDetails.IBAN;
                            existingBankDetails.SwiftCode = subBrokerage.BankDetails.SwiftCode;
                        }
                        else
                        {
                            // Create new bank details
                            var subBrokerageBankDetails = new CE_BankDetails
                            {
                                EntityType = "SubBrokerage",
                                EntityID = subBrokerageEntity.SubBrokerageID,
                                BankName = subBrokerage.BankDetails.BankName,
                                BankBranchAddress = subBrokerage.BankDetails.BankBranchAddress,
                                ReceiversAddress = subBrokerage.BankDetails.ReceiversAddress,
                                AccountName = subBrokerage.BankDetails.AccountName,
                                AccountSortCode = subBrokerage.BankDetails.AccountSortCode,
                                AccountNumber = subBrokerage.BankDetails.AccountNumber,
                                IBAN = subBrokerage.BankDetails.IBAN,
                                SwiftCode = subBrokerage.BankDetails.SwiftCode
                            };
                            db.CE_BankDetails.Add(subBrokerageBankDetails);
                        }
                    }

                    // Update/Create Company Tax Info if provided
                    if (subBrokerage.CompanyTaxInfo != null && !string.IsNullOrEmpty(subBrokerage.CompanyTaxInfo.CompanyRegistration))
                    {
                        var existingCompanyTaxInfo = await db.CE_CompanyTaxInfo
                            .FirstOrDefaultAsync(c => c.EntityType == "SubBrokerage" && c.EntityID == subBrokerageEntity.SubBrokerageID);

                        if (existingCompanyTaxInfo != null)
                        {
                            // Update existing company tax info
                            existingCompanyTaxInfo.CompanyRegistration = subBrokerage.CompanyTaxInfo.CompanyRegistration;
                            existingCompanyTaxInfo.VATNumber = subBrokerage.CompanyTaxInfo.VATNumber;
                            existingCompanyTaxInfo.Notes = subBrokerage.CompanyTaxInfo.Notes;
                        }
                        else
                        {
                            // Create new company tax info
                            var subBrokerageCompanyTaxInfo = new CE_CompanyTaxInfo
                            {
                                EntityType = "SubBrokerage",
                                EntityID = subBrokerageEntity.SubBrokerageID,
                                CompanyRegistration = subBrokerage.CompanyTaxInfo.CompanyRegistration,
                                VATNumber = subBrokerage.CompanyTaxInfo.VATNumber,
                                Notes = subBrokerage.CompanyTaxInfo.Notes
                            };
                            db.CE_CompanyTaxInfo.Add(subBrokerageCompanyTaxInfo);
                        }
                    }

                    // Add new commissions
                    if (subBrokerage.Commissions != null && subBrokerage.Commissions.Any())
                    {
                        foreach (var commission in subBrokerage.Commissions.Where(c => c != null))
                        {
                            var subBrokerageCommission = new CE_SubBrokerageCommissionAndPayment
                            {
                                SubBrokerageID = subBrokerageEntity.SubBrokerageID,
                                SubBrokerageCommissionPercent = commission.SubBrokerageCommission,
                                BrokerageCommissionPercent = commission.BrokerageCommission,
                                SubBrokerageStartDate = !string.IsNullOrEmpty(commission.SubBrokerageStartDate) ? DateTime.Parse(commission.SubBrokerageStartDate) : (DateTime?)null,
                                SubBrokerageEndDate = !string.IsNullOrEmpty(commission.SubBrokerageEndDate) ? DateTime.Parse(commission.SubBrokerageEndDate) : (DateTime?)null,
                                BrokerageStartDate = !string.IsNullOrEmpty(commission.BrokerageStartDate) ? DateTime.Parse(commission.BrokerageStartDate) : (DateTime?)null,
                                BrokerageEndDate = !string.IsNullOrEmpty(commission.BrokerageEndDate) ? DateTime.Parse(commission.BrokerageEndDate) : (DateTime?)null,
                                PaymentTerms = commission.PaymentTerms,
                                CommissionType = commission.CommissionType
                            };
                            db.CE_SubBrokerageCommissionAndPayment.Add(subBrokerageCommission);
                        }
                    }
                }
            }

            // Update Sub Referrals - Intelligent update strategy to preserve existing data
            if (model.SubReferrals != null && model.SubReferrals.Any(s => !string.IsNullOrEmpty(s.SubReferralPartnerName)))
            {
                var existingSubReferrals = await db.CE_SubReferral
                    .Where(s => s.SectorID == sectorId)
                    .ToListAsync();

                var submittedNames = model.SubReferrals
                    .Where(s => !string.IsNullOrEmpty(s.SubReferralPartnerName))
                    .Select(s => s.SubReferralPartnerName)
                    .ToList();

                // Remove SubReferrals that are no longer present in the submission
                var subReferralsToRemove = existingSubReferrals
                    .Where(existing => !submittedNames.Contains(existing.SubReferralPartnerName))
                    .ToList();

                if (subReferralsToRemove.Any())
                {
                    var removeIds = subReferralsToRemove.Select(s => s.SubReferralID).ToList();

                    // Remove related data for truly deleted SubReferrals
                    var bankDetailsToRemove = await db.CE_BankDetails
                        .Where(b => b.EntityType == "SubReferral" && removeIds.Contains(b.EntityID))
                        .ToListAsync();
                    db.CE_BankDetails.RemoveRange(bankDetailsToRemove);

                    var companyTaxToRemove = await db.CE_CompanyTaxInfo
                        .Where(c => c.EntityType == "SubReferral" && removeIds.Contains(c.EntityID))
                        .ToListAsync();
                    db.CE_CompanyTaxInfo.RemoveRange(companyTaxToRemove);

                    var commissionsToRemove = await db.CE_SubReferralCommissionAndPayment
                        .Where(c => removeIds.Contains(c.SubReferralID))
                        .ToListAsync();
                    db.CE_SubReferralCommissionAndPayment.RemoveRange(commissionsToRemove);

                    db.CE_SubReferral.RemoveRange(subReferralsToRemove);
                }

                // Process each submitted SubReferral
                foreach (var subReferral in model.SubReferrals.Where(s => s != null && !string.IsNullOrEmpty(s.SubReferralPartnerName)))
                {
                    var existingSubReferral = existingSubReferrals
                        .FirstOrDefault(e => e.SubReferralPartnerName == subReferral.SubReferralPartnerName);

                    CE_SubReferral subReferralEntity;

                    if (existingSubReferral != null)
                    {
                        // Update existing SubReferral
                        existingSubReferral.Active = subReferral.Active;
                        existingSubReferral.StartDate = !string.IsNullOrEmpty(subReferral.StartDate) ? DateTime.Parse(subReferral.StartDate) : (DateTime?)null;
                        existingSubReferral.EndDate = !string.IsNullOrEmpty(subReferral.EndDate) ? DateTime.Parse(subReferral.EndDate) : (DateTime?)null;
                        existingSubReferral.SubReferralPartnerEmail = subReferral.Email ?? "";
                        existingSubReferral.SubReferralPartnerLandline = subReferral.Landline ?? "";
                        existingSubReferral.SubReferralPartnerMobile = subReferral.Mobile ?? "";
                        subReferralEntity = existingSubReferral;

                        // Update commissions (replace all)
                        var existingCommissions = await db.CE_SubReferralCommissionAndPayment
                            .Where(c => c.SubReferralID == existingSubReferral.SubReferralID)
                            .ToListAsync();
                        db.CE_SubReferralCommissionAndPayment.RemoveRange(existingCommissions);
                    }
                    else
                    {
                        // Create new SubReferral
                        subReferralEntity = new CE_SubReferral
                        {
                            SectorID = sectorId,
                            SubReferralPartnerName = subReferral.SubReferralPartnerName,
                            Active = subReferral.Active,
                            StartDate = !string.IsNullOrEmpty(subReferral.StartDate) ? DateTime.Parse(subReferral.StartDate) : (DateTime?)null,
                            EndDate = !string.IsNullOrEmpty(subReferral.EndDate) ? DateTime.Parse(subReferral.EndDate) : (DateTime?)null,
                            SubReferralPartnerEmail = subReferral.Email ?? "",
                            SubReferralPartnerLandline = subReferral.Landline ?? "",
                            SubReferralPartnerMobile = subReferral.Mobile ?? ""
                        };
                        db.CE_SubReferral.Add(subReferralEntity);
                        await db.SaveChangesAsync(); // Save to get ID for new entity
                    }

                    // Update/Create Bank Details if provided
                    if (subReferral.BankDetails != null && !string.IsNullOrEmpty(subReferral.BankDetails.BankName))
                    {
                        var existingBankDetails = await db.CE_BankDetails
                            .FirstOrDefaultAsync(b => b.EntityType == "SubReferral" && b.EntityID == subReferralEntity.SubReferralID);

                        if (existingBankDetails != null)
                        {
                            // Update existing bank details
                            existingBankDetails.BankName = subReferral.BankDetails.BankName;
                            existingBankDetails.BankBranchAddress = subReferral.BankDetails.BankBranchAddress;
                            existingBankDetails.ReceiversAddress = subReferral.BankDetails.ReceiversAddress;
                            existingBankDetails.AccountName = subReferral.BankDetails.AccountName;
                            existingBankDetails.AccountSortCode = subReferral.BankDetails.AccountSortCode;
                            existingBankDetails.AccountNumber = subReferral.BankDetails.AccountNumber;
                            existingBankDetails.IBAN = subReferral.BankDetails.IBAN;
                            existingBankDetails.SwiftCode = subReferral.BankDetails.SwiftCode;
                        }
                        else
                        {
                            // Create new bank details
                            var subReferralBankDetails = new CE_BankDetails
                            {
                                EntityType = "SubReferral",
                                EntityID = subReferralEntity.SubReferralID,
                                BankName = subReferral.BankDetails.BankName,
                                BankBranchAddress = subReferral.BankDetails.BankBranchAddress,
                                ReceiversAddress = subReferral.BankDetails.ReceiversAddress,
                                AccountName = subReferral.BankDetails.AccountName,
                                AccountSortCode = subReferral.BankDetails.AccountSortCode,
                                AccountNumber = subReferral.BankDetails.AccountNumber,
                                IBAN = subReferral.BankDetails.IBAN,
                                SwiftCode = subReferral.BankDetails.SwiftCode
                            };
                            db.CE_BankDetails.Add(subReferralBankDetails);
                        }
                    }

                    // Update/Create Company Tax Info if provided
                    if (subReferral.CompanyTaxInfo != null && !string.IsNullOrEmpty(subReferral.CompanyTaxInfo.CompanyRegistration))
                    {
                        var existingCompanyTaxInfo = await db.CE_CompanyTaxInfo
                            .FirstOrDefaultAsync(c => c.EntityType == "SubReferral" && c.EntityID == subReferralEntity.SubReferralID);

                        if (existingCompanyTaxInfo != null)
                        {
                            // Update existing company tax info
                            existingCompanyTaxInfo.CompanyRegistration = subReferral.CompanyTaxInfo.CompanyRegistration;
                            existingCompanyTaxInfo.VATNumber = subReferral.CompanyTaxInfo.VATNumber;
                            existingCompanyTaxInfo.Notes = subReferral.CompanyTaxInfo.Notes;
                        }
                        else
                        {
                            // Create new company tax info
                            var subReferralCompanyTaxInfo = new CE_CompanyTaxInfo
                            {
                                EntityType = "SubReferral",
                                EntityID = subReferralEntity.SubReferralID,
                                CompanyRegistration = subReferral.CompanyTaxInfo.CompanyRegistration,
                                VATNumber = subReferral.CompanyTaxInfo.VATNumber,
                                Notes = subReferral.CompanyTaxInfo.Notes
                            };
                            db.CE_CompanyTaxInfo.Add(subReferralCompanyTaxInfo);
                        }
                    }

                    // Add new commissions
                    if (subReferral.Commissions != null && subReferral.Commissions.Any())
                    {
                        foreach (var commission in subReferral.Commissions.Where(c => c != null))
                        {
                            var subReferralCommission = new CE_SubReferralCommissionAndPayment
                            {
                                SubReferralID = subReferralEntity.SubReferralID,
                                SubIntroducerCommission = commission.SubReferralCommission, // ViewModel.SubReferralCommission -> DB.SubIntroducerCommission
                                IntroducerCommission = commission.ReferralPartnerCommission, // ViewModel.ReferralPartnerCommission -> DB.IntroducerCommission
                                SubIntroducerStartDate = !string.IsNullOrEmpty(commission.SubReferralStartDate) ? DateTime.Parse(commission.SubReferralStartDate) : (DateTime?)null,
                                SubIntroducerEndDate = !string.IsNullOrEmpty(commission.SubReferralEndDate) ? DateTime.Parse(commission.SubReferralEndDate) : (DateTime?)null,
                                IntroducerStartDate = !string.IsNullOrEmpty(commission.ReferralPartnerStartDate) ? DateTime.Parse(commission.ReferralPartnerStartDate) : (DateTime?)null,
                                IntroducerEndDate = !string.IsNullOrEmpty(commission.ReferralPartnerEndDate) ? DateTime.Parse(commission.ReferralPartnerEndDate) : (DateTime?)null,
                                PaymentTerms = commission.PaymentTerms,
                                CommissionType = commission.CommissionType
                            };
                            db.CE_SubReferralCommissionAndPayment.Add(subReferralCommission);
                        }
                    }
                }
            }

            // Update Sub Introducers - Intelligent update strategy to preserve existing data
            if (model.SubIntroducers != null && model.SubIntroducers.Any(s => !string.IsNullOrEmpty(s.SubIntroducerName)))
            {
                var existingSubIntroducers = await db.CE_SubIntroducer
                    .Where(s => s.SectorID == sectorId)
                    .ToListAsync();

                var submittedNames = model.SubIntroducers
                    .Where(s => !string.IsNullOrEmpty(s.SubIntroducerName))
                    .Select(s => s.SubIntroducerName)
                    .ToList();

                // Remove SubIntroducers that are no longer present in the submission
                var subIntroducersToRemove = existingSubIntroducers
                    .Where(existing => !submittedNames.Contains(existing.SubIntroducerName))
                    .ToList();

                if (subIntroducersToRemove.Any())
                {
                    var removeIds = subIntroducersToRemove.Select(s => s.SubIntroducerID).ToList();

                    // Remove related data for truly deleted SubIntroducers
                    var bankDetailsToRemove = await db.CE_BankDetails
                        .Where(b => b.EntityType == "SubIntroducer" && removeIds.Contains(b.EntityID))
                        .ToListAsync();
                    db.CE_BankDetails.RemoveRange(bankDetailsToRemove);

                    var companyTaxToRemove = await db.CE_CompanyTaxInfo
                        .Where(c => c.EntityType == "SubIntroducer" && removeIds.Contains(c.EntityID))
                        .ToListAsync();
                    db.CE_CompanyTaxInfo.RemoveRange(companyTaxToRemove);

                    var commissionsToRemove = await db.CE_SubIntroducerCommissionAndPayment
                        .Where(c => removeIds.Contains(c.SubIntroducerID))
                        .ToListAsync();
                    db.CE_SubIntroducerCommissionAndPayment.RemoveRange(commissionsToRemove);

                    db.CE_SubIntroducer.RemoveRange(subIntroducersToRemove);
                }

                // Process each submitted SubIntroducer
                foreach (var subIntroducer in model.SubIntroducers.Where(s => s != null && !string.IsNullOrEmpty(s.SubIntroducerName)))
                {
                    var existingSubIntroducer = existingSubIntroducers
                        .FirstOrDefault(e => e.SubIntroducerName == subIntroducer.SubIntroducerName);

                    CE_SubIntroducer subIntroducerEntity;

                    if (existingSubIntroducer != null)
                    {
                        // Update existing SubIntroducer
                        existingSubIntroducer.OfgemID = subIntroducer.OfgemID ?? "";
                        existingSubIntroducer.Active = subIntroducer.Active;
                        existingSubIntroducer.StartDate = !string.IsNullOrEmpty(subIntroducer.StartDate) ? DateTime.Parse(subIntroducer.StartDate) : (DateTime?)null;
                        existingSubIntroducer.EndDate = !string.IsNullOrEmpty(subIntroducer.EndDate) ? DateTime.Parse(subIntroducer.EndDate) : (DateTime?)null;
                        existingSubIntroducer.SubIntroducerEmail = subIntroducer.Email ?? "";
                        existingSubIntroducer.SubIntroducerLandline = subIntroducer.Landline ?? "";
                        existingSubIntroducer.SubIntroducerMobile = subIntroducer.Mobile ?? "";
                        subIntroducerEntity = existingSubIntroducer;

                        // Update commissions (replace all)
                        var existingCommissions = await db.CE_SubIntroducerCommissionAndPayment
                            .Where(c => c.SubIntroducerID == existingSubIntroducer.SubIntroducerID)
                            .ToListAsync();
                        db.CE_SubIntroducerCommissionAndPayment.RemoveRange(existingCommissions);
                    }
                    else
                    {
                        // Create new SubIntroducer
                        subIntroducerEntity = new CE_SubIntroducer
                        {
                            SectorID = sectorId,
                            SubIntroducerName = subIntroducer.SubIntroducerName,
                            OfgemID = subIntroducer.OfgemID ?? "",
                            Active = subIntroducer.Active,
                            StartDate = !string.IsNullOrEmpty(subIntroducer.StartDate) ? DateTime.Parse(subIntroducer.StartDate) : (DateTime?)null,
                            EndDate = !string.IsNullOrEmpty(subIntroducer.EndDate) ? DateTime.Parse(subIntroducer.EndDate) : (DateTime?)null,
                            SubIntroducerEmail = subIntroducer.Email ?? "",
                            SubIntroducerLandline = subIntroducer.Landline ?? "",
                            SubIntroducerMobile = subIntroducer.Mobile ?? ""
                        };
                        db.CE_SubIntroducer.Add(subIntroducerEntity);
                        await db.SaveChangesAsync(); // Save to get ID for new entity
                    }

                    // Update/Create Bank Details if provided
                    if (subIntroducer.BankDetails != null && !string.IsNullOrEmpty(subIntroducer.BankDetails.BankName))
                    {
                        var existingBankDetails = await db.CE_BankDetails
                            .FirstOrDefaultAsync(b => b.EntityType == "SubIntroducer" && b.EntityID == subIntroducerEntity.SubIntroducerID);

                        if (existingBankDetails != null)
                        {
                            // Update existing bank details
                            existingBankDetails.BankName = subIntroducer.BankDetails.BankName;
                            existingBankDetails.BankBranchAddress = subIntroducer.BankDetails.BankBranchAddress;
                            existingBankDetails.ReceiversAddress = subIntroducer.BankDetails.ReceiversAddress;
                            existingBankDetails.AccountName = subIntroducer.BankDetails.AccountName;
                            existingBankDetails.AccountSortCode = subIntroducer.BankDetails.AccountSortCode;
                            existingBankDetails.AccountNumber = subIntroducer.BankDetails.AccountNumber;
                            existingBankDetails.IBAN = subIntroducer.BankDetails.IBAN;
                            existingBankDetails.SwiftCode = subIntroducer.BankDetails.SwiftCode;
                        }
                        else
                        {
                            // Create new bank details
                            var subIntroducerBankDetails = new CE_BankDetails
                            {
                                EntityType = "SubIntroducer",
                                EntityID = subIntroducerEntity.SubIntroducerID,
                                BankName = subIntroducer.BankDetails.BankName,
                                BankBranchAddress = subIntroducer.BankDetails.BankBranchAddress,
                                ReceiversAddress = subIntroducer.BankDetails.ReceiversAddress,
                                AccountName = subIntroducer.BankDetails.AccountName,
                                AccountSortCode = subIntroducer.BankDetails.AccountSortCode,
                                AccountNumber = subIntroducer.BankDetails.AccountNumber,
                                IBAN = subIntroducer.BankDetails.IBAN,
                                SwiftCode = subIntroducer.BankDetails.SwiftCode
                            };
                            db.CE_BankDetails.Add(subIntroducerBankDetails);
                        }
                    }

                    // Update/Create Company Tax Info if provided
                    if (subIntroducer.CompanyTaxInfo != null && !string.IsNullOrEmpty(subIntroducer.CompanyTaxInfo.CompanyRegistration))
                    {
                        var existingCompanyTaxInfo = await db.CE_CompanyTaxInfo
                            .FirstOrDefaultAsync(c => c.EntityType == "SubIntroducer" && c.EntityID == subIntroducerEntity.SubIntroducerID);

                        if (existingCompanyTaxInfo != null)
                        {
                            // Update existing company tax info
                            existingCompanyTaxInfo.CompanyRegistration = subIntroducer.CompanyTaxInfo.CompanyRegistration;
                            existingCompanyTaxInfo.VATNumber = subIntroducer.CompanyTaxInfo.VATNumber;
                            existingCompanyTaxInfo.Notes = subIntroducer.CompanyTaxInfo.Notes;
                        }
                        else
                        {
                            // Create new company tax info
                            var subIntroducerCompanyTaxInfo = new CE_CompanyTaxInfo
                            {
                                EntityType = "SubIntroducer",
                                EntityID = subIntroducerEntity.SubIntroducerID,
                                CompanyRegistration = subIntroducer.CompanyTaxInfo.CompanyRegistration,
                                VATNumber = subIntroducer.CompanyTaxInfo.VATNumber,
                                Notes = subIntroducer.CompanyTaxInfo.Notes
                            };
                            db.CE_CompanyTaxInfo.Add(subIntroducerCompanyTaxInfo);
                        }
                    }

                    // Add new commissions
                    if (subIntroducer.Commissions != null && subIntroducer.Commissions.Any())
                    {
                        foreach (var commission in subIntroducer.Commissions.Where(c => c != null))
                        {
                            var subIntroducerCommission = new CE_SubIntroducerCommissionAndPayment
                            {
                                SubIntroducerID = subIntroducerEntity.SubIntroducerID,
                                SubIntroducerCommission = commission.SubIntroducerCommission,
                                IntroducerCommission = commission.IntroducerCommission,
                                SubIntroducerCommissionStartDate = !string.IsNullOrEmpty(commission.SubIntroducerStartDate) ? DateTime.Parse(commission.SubIntroducerStartDate) : (DateTime?)null,
                                SubIntroducerCommissionEndDate = !string.IsNullOrEmpty(commission.SubIntroducerEndDate) ? DateTime.Parse(commission.SubIntroducerEndDate) : (DateTime?)null,
                                IntroducerCommissionStartDate = !string.IsNullOrEmpty(commission.IntroducerStartDate) ? DateTime.Parse(commission.IntroducerStartDate) : (DateTime?)null,
                                IntroducerCommissionEndDate = !string.IsNullOrEmpty(commission.IntroducerEndDate) ? DateTime.Parse(commission.IntroducerEndDate) : (DateTime?)null,
                                PaymentTerms = commission.PaymentTerms,
                                CommissionType = commission.CommissionType
                            };
                            db.CE_SubIntroducerCommissionAndPayment.Add(subIntroducerCommission);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handles sector supplier relationships - removes existing and adds new ones
        /// </summary>
        private async Task HandleSectorSuppliers(int sectorId, List<long> supplierIds)
        {
            try
            {
                // Remove existing relationships
                var existingSuppliers = await db.CE_SectorSupplier
                    .Where(ss => ss.SectorId == sectorId)
                    .ToListAsync();

                db.CE_SectorSupplier.RemoveRange(existingSuppliers);

                // Add new relationships
                if (supplierIds != null && supplierIds.Any())
                {
                    var sectorSuppliers = supplierIds.Select(supplierId => new CE_SectorSupplier
                    {
                        SectorId = sectorId,
                        SupplierId = supplierId
                    }).ToList();

                    db.CE_SectorSupplier.AddRange(sectorSuppliers);
                }

                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Logic.Logger.Log($"HandleSectorSuppliers failed: {ex.Message}");
                throw;
            }
        }

        #endregion
    }
}
