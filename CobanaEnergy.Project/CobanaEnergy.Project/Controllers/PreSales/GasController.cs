using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Filters;
using CobanaEnergy.Project.Helpers;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.AccountsDBModel;
using CobanaEnergy.Project.Models.Business;
using CobanaEnergy.Project.Models.Gas;
using CobanaEnergy.Project.Models.Gas.EditGas;
using CobanaEnergy.Project.Models.Gas.GasDBModels;
using CobanaEnergy.Project.Models.Supplier.SupplierDBModels.snapshot_Gas;
using CobanaEnergy.Project.Models.Supplier.SupplierSnapshots_Gas;
using Logic;
using Logic.LockManager;
using Logic.ResponseModel.Helper;
using Microsoft.AspNet.Identity;
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
    public class GasController : BaseController
    {
        private readonly ApplicationDBContext db;

        public GasController(ApplicationDBContext _db)
        {
            db = _db;
        }

        #region create_gas

        [HttpGet]
        [Authorize(Roles = "Pre-sales,Controls")]
        public async Task<ActionResult> CreateGas()
        {
            return View("CreateGas");
        }

        [HttpPost]
        [Authorize(Roles = "Pre-sales,Controls")]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> CreateGas(GasContractCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                             .Where(x => x.Value.Errors.Count > 0)
                             .SelectMany(kvp => kvp.Value.Errors)
                             .Select(e => e.ErrorMessage)
                             .ToList();

                string combinedError = string.Join("<br>", errors);
                return JsonResponse.Fail(combinedError);
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var guid = Guid.NewGuid().ToString().ToUpper();

                    var contract = new CE_GasContracts
                    {
                        EId = guid,
                        MPRN = model.MPRN,
                        BusinessName = model.BusinessName,
                        CustomerName = model.CustomerName,
                        BusinessDoorNumber = model.BusinessDoorNumber,
                        BusinessHouseName = model.BusinessHouseName,
                        BusinessStreet = model.BusinessStreet,
                        BusinessTown = model.BusinessTown,
                        BusinessCounty = model.BusinessCounty,
                        PostCode = model.PostCode,
                        PhoneNumber1 = model.PhoneNumber1,
                        PhoneNumber2 = model.PhoneNumber2,
                        EmailAddress = model.EmailAddress,
                        InputDate = DateTime.Now.ToString("dd/MM/yyyy"),
                        InitialStartDate = model.InitialStartDate,
                        Uplift = model.Uplift.ToString(),
                        Duration = model.Duration,
                        InputEAC = model.InputEAC.ToString(),
                        UnitRate = model.UnitRate.ToString(),
                        OtherRate = model.OtherRate.ToString(),
                        StandingCharge = model.StandingCharge.ToString(),
                        //SortCode = model.SortCode,
                        // AccountNumber = model.AccountNumber,
                        CurrentSupplier = model.CurrentSupplier,
                        SupplierId = model.SupplierId,
                        ProductId = model.ProductId,
                        EMProcessor = model.EMProcessor,
                        ContractChecked = model.ContractChecked,
                        ContractAudited = model.ContractAudited,
                        Terminated = model.Terminated,
                        ContractNotes = model.ContractNotes,
                        CreatedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                        UpdatedAt = "000",
                        Type = "Gas",
                        Department = model.Department,
                        Source = model.Source,
                        SalesType = model.SalesType,
                        SalesTypeStatus = model.SalesTypeStatus,
                        SupplierCommsType = model.SupplierCommsType,
                        PreSalesStatus = model.PreSalesStatus,
                        PreSalesFollowUpDate = DateTime.TryParse(model.PreSalesFollowUpDate, out DateTime presalesDate) ? presalesDate : (DateTime?)null,

                        // Brokerage Details
                        BrokerageId = model.BrokerageId,
                        OfgemId = model.OfgemId,

                        // Dynamic Department-based fields
                        CloserId = model.CloserId,
                        ReferralPartnerId = model.ReferralPartnerId,
                        SubReferralPartnerId = model.SubReferralPartnerId,
                        BrokerageStaffId = model.BrokerageStaffId,
                        IntroducerId = model.IntroducerId,
                        SubIntroducerId = model.SubIntroducerId,
                        SubBrokerageId = model.SubBrokerageId,
                        Collaboration = model.Collaboration,
                        LeadGeneratorId = model.LeadGeneratorId
                    };

                    db.CE_GasContracts.Add(contract);
                    await db.SaveChangesAsync();

                    db.CE_Accounts.Add(new CE_Accounts
                    {
                        Type = "Gas",
                        EId = guid,
                        SortCode = model.SortCode,
                        AccountNumber = model.AccountNumber
                    });

                    string username = model.EMProcessor ?? "PreSales Team";
                    string now = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                    db.CE_GasContractLogs.Add(new CE_GasContractLogs
                    {
                        EId = guid,
                        Username = username,
                        ActionDate = now,
                        PreSalesStatusType = model.PreSalesStatus,
                        Message = "Gas Contract created."
                    });

                    if (model.ContractChecked)
                    {
                        db.CE_GasContractLogs.Add(new CE_GasContractLogs
                        {
                            EId = guid,
                            Username = username,
                            ActionDate = now,
                            PreSalesStatusType = model.PreSalesStatus,
                            Message = "Contract has been checked."
                        });
                    }

                    if (model.ContractAudited)
                    {
                        db.CE_GasContractLogs.Add(new CE_GasContractLogs
                        {
                            EId = guid,
                            Username = username,
                            ActionDate = now,
                            PreSalesStatusType = model.PreSalesStatus,
                            Message = "Contract has been audited."
                        });
                    }

                    if (model.Terminated)
                    {
                        db.CE_GasContractLogs.Add(new CE_GasContractLogs
                        {
                            EId = guid,
                            Username = username,
                            ActionDate = now,
                            PreSalesStatusType = model.PreSalesStatus,
                            Message = "Contract has been marked as terminated."
                        });
                    }

                    await db.SaveChangesAsync();

                    var supplierId = model.SupplierId;
                    var supplier = await db.CE_Supplier.FirstOrDefaultAsync(s => s.Id == supplierId);

                    if (supplier == null)
                    {
                        Logger.Log($"Supplier ID {supplierId} not found while creating snapshot.");
                        throw new Exception("Supplier not found for snapshot.");
                    }

                    var snapshot = new CE_GasSupplierSnapshots
                    {
                        EId = guid,
                        SupplierId = supplierId,
                        SupplierName = supplier.Name,
                        SupplierLink = supplier.Link,
                        CE_GasSupplierProductSnapshots = new List<CE_GasSupplierProductSnapshots>(),
                        CE_GasSupplierContactSnapshots = new List<CE_GasSupplierContactSnapshots>(),
                        CE_GasSupplierUpliftSnapshots = new List<CE_GasSupplierUpliftSnapshots>()
                    };

                    var products = await db.CE_SupplierProducts
                        .Where(p => p.SupplierId == supplierId)
                        .ToListAsync();

                    foreach (var p in products)
                    {                        
                        snapshot.CE_GasSupplierProductSnapshots.Add(new CE_GasSupplierProductSnapshots
                        {
                            SupplierId = supplierId,
                            ProductId = p.Id,
                            ProductName = p.ProductName,
                            StartDate = DateTime.Parse(p.StartDate),
                            EndDate = DateTime.Parse(p.EndDate),
                            Commission = p.Commission,
                            SupplierCommsType = p.SupplierCommsType
                        });
                    }

                    var contacts = await db.CE_SupplierContacts
                        .Where(c => c.SupplierId == supplierId)
                        .ToListAsync();

                    foreach (var c in contacts)
                    {
                        snapshot.CE_GasSupplierContactSnapshots.Add(new CE_GasSupplierContactSnapshots
                        {
                            SupplierId = supplierId,
                            ContactName = c.ContactName,
                            Role = c.Role,
                            PhoneNumber = c.PhoneNumber,
                            Email = c.Email,
                            Notes = c.Notes
                        });
                    }

                    var uplifts = await db.CE_SupplierUplifts
                        .Where(u => u.SupplierId == supplierId && u.FuelType == "Gas")
                        .ToListAsync();

                    foreach (var u in uplifts)
                    {
                        snapshot.CE_GasSupplierUpliftSnapshots.Add(new CE_GasSupplierUpliftSnapshots
                        {
                            SupplierId = supplierId,
                            FuelType = u.FuelType,
                            Uplift = u.Uplift,
                            StartDate = u.StartDate,
                            EndDate = u.EndDate
                        });
                    }

                    db.CE_GasSupplierSnapshots.Add(snapshot);
                    await db.SaveChangesAsync();


                    // Add Business Contact Info

                    db.CE_BusinessContactInfos.Add(new CE_BusinessContactInfo
                    {
                        EId = contract.EId,
                        Type = contract.Type,
                        BusinessName = contract.BusinessName,
                        CustomerName = contract.CustomerName,
                        PhoneNumber1 = contract.PhoneNumber1,
                        PhoneNumber2 = contract.PhoneNumber2,
                        EmailAddress = contract.EmailAddress

                    });

                    await db.SaveChangesAsync();

                    transaction.Commit();

                    return JsonResponse.Ok(new { redirectUrl = Url.Action("Dashboard", "PreSales") }, "Gas contract created successfully!");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Logger.Log("CreateGas failed: " + ex.Message);
                    return JsonResponse.Fail("An unexpected error occurred while saving the gas contract.");
                }
            }
        }

        #endregion

        #region CheckDuplicateMprn

        [HttpGet]
        [Authorize]
        public async Task<JsonResult> CheckDuplicateMprn(string mprn)
        {
            if (string.IsNullOrWhiteSpace(mprn) || mprn.Length < 6 || mprn.Length > 10 || !mprn.All(char.IsDigit))
                return JsonResponse.Fail("Invalid MPRN format");

            try
            {
                var match = await db.CE_GasContracts
                    .Where(c => c.MPRN == mprn)
                    .OrderByDescending(c => c.InputDate)
                    .FirstOrDefaultAsync();

                if (match != null)
                {
                    // Determine Agent based on department type
                    string agentName = "-";
                    
                    if (match.Department == "In House" && match.CloserId.HasValue)
                    {
                        var closer = await db.CE_Sector
                            .Where(s => s.SectorID == match.CloserId && s.SectorType == "closer")
                            .Select(s => s.Name)
                            .FirstOrDefaultAsync();
                        agentName = closer ?? "-";
                    }
                    else if (match.Department == "Brokers" && match.BrokerageStaffId.HasValue)
                    {
                        var brokerageStaff = await db.CE_BrokerageStaff
                            .Where(bs => bs.BrokerageStaffID == match.BrokerageStaffId)
                            .Select(bs => bs.BrokerageStaffName)
                            .FirstOrDefaultAsync();
                        agentName = brokerageStaff ?? "-";
                    }
                    else if (match.Department == "Introducers" && match.SubIntroducerId.HasValue)
                    {
                        var subIntroducer = await db.CE_SubIntroducer
                            .Where(si => si.SubIntroducerID == match.SubIntroducerId)
                            .Select(si => si.SubIntroducerName)
                            .FirstOrDefaultAsync();
                        agentName = subIntroducer ?? "-";
                    }

                    var result = new
                    {
                        match.BusinessName,
                        match.CustomerName,
                        InputDate = DateTime.TryParse(match.InputDate, out DateTime parsedDate)
                                     ? parsedDate.ToString("dd/MM/yyyy")
                                     : match.InputDate,
                        match.PreSalesStatus,
                        match.Duration,
                        Agent = agentName
                    };

                    return JsonResponse.Ok(result);
                }
                else
                {
                    return JsonResponse.Ok(null);
                }
            }
            catch (Exception ex)
            {
                Logger.Log("CheckDuplicateMprn failed: " + ex.Message);
                return JsonResponse.Fail("Error occurred while checking MPRN.");
            }
        }

        #endregion

        #region AutoComplete MPRN

        [HttpGet]
        [Authorize]
        public async Task<JsonResult> GetMPRNs(string search)
        {
            try
            {
                var matches = await db.CE_GasContracts
               .Where(c => c.MPRN.StartsWith(search)).Select(c => c.MPRN)
               .ToListAsync();
                return JsonResponse.Ok(matches);
            }
            catch (Exception ex)
            {
                Logger.Log("Failed to search MPRN(s): " + ex.Message);
                return JsonResponse.Fail("An unexpected error occurred while searching MPRN(s).");
            }


        }


        [HttpGet]
        [Authorize]
        public async Task<JsonResult> GetMPRNRelationalData(string mprn)
        {
            if (string.IsNullOrWhiteSpace(mprn) || mprn.Length < 6 || mprn.Length > 10 || !mprn.All(char.IsDigit))
                return JsonResponse.Fail("Invalid MPRN format");

            try
            {
                var match = await db.CE_GasContracts
                   .Where(c => c.MPRN == mprn)
                   .OrderByDescending(c => c.InputDate)
                   .FirstOrDefaultAsync();

                if (match != null)
                {
                    var result = new
                    {
                        match.BusinessDoorNumber,
                        match.BusinessHouseName,
                        match.BusinessStreet,
                        match.BusinessTown,
                        match.BusinessCounty,
                        match.PostCode
                    };

                    return JsonResponse.Ok(result);
                }
                else
                {
                    return JsonResponse.Ok(null);
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Failed to get MPRN releational data: " + ex.Message);
                return JsonResponse.Fail("Error occurred while fetching MPRN relational data.");
            }
        }

        #endregion

        #region Edit_Gas

        [HttpGet]
        [Authorize(Roles = "Pre-sales,Controls")]
        public async Task<ActionResult> EditGas(string eid)
        {
            if (string.IsNullOrWhiteSpace(eid))
                return HttpNotFound();
            try
            {
                var contract = await db.CE_GasContracts.FirstOrDefaultAsync(c => c.EId == eid);
                var account = await db.CE_Accounts.FirstOrDefaultAsync(a => a.EId == eid && a.Type == "Gas");

                if (contract == null)
                    return HttpNotFound();

                var snapshot = await db.CE_GasSupplierSnapshots
                                .Include(s => s.CE_GasSupplierProductSnapshots)
                                .Include(s => s.CE_GasSupplierContactSnapshots)
                                .Include(s => s.CE_GasSupplierUpliftSnapshots)
                                .FirstOrDefaultAsync(s => s.EId == contract.EId);

                if (snapshot == null)
                    return HttpNotFound();

                var savedProductSnapshot = snapshot.CE_GasSupplierProductSnapshots
                                           .FirstOrDefault(p => p.ProductId == contract.ProductId);

                var model = new GasContractEditViewModel
                {
                    EId = contract.EId,
                    MPRN = contract.MPRN,
                    BusinessName = contract.BusinessName,
                    CustomerName = contract.CustomerName,
                    BusinessDoorNumber = contract.BusinessDoorNumber,
                    BusinessHouseName = contract.BusinessHouseName,
                    BusinessStreet = contract.BusinessStreet,
                    BusinessTown = contract.BusinessTown,
                    BusinessCounty = contract.BusinessCounty,
                    PostCode = contract.PostCode,
                    PhoneNumber1 = contract.PhoneNumber1,
                    PhoneNumber2 = contract.PhoneNumber2,
                    EmailAddress = contract.EmailAddress,
                    InitialStartDate = contract.InitialStartDate,
                    InputDate = contract.InputDate,
                    Uplift = Convert.ToDecimal(contract.Uplift),
                    Duration = contract.Duration,
                    InputEAC = Convert.ToDecimal(contract.InputEAC),
                    UnitRate = Convert.ToDecimal(contract.UnitRate),
                    OtherRate = Convert.ToDecimal(contract.OtherRate),
                    StandingCharge = Convert.ToDecimal(contract.StandingCharge),
                    SortCode = account?.SortCode ?? "",
                    AccountNumber = account?.AccountNumber ?? "",
                    CurrentSupplier = contract.CurrentSupplier,

                    SupplierId = snapshot.SupplierId,
                    ProductId = savedProductSnapshot?.Id ?? 0,
                    SupplierCommsType = savedProductSnapshot?.SupplierCommsType ?? contract.SupplierCommsType,

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
                    PreSalesFollowUpDate = contract.PreSalesFollowUpDate?.ToString("yyyy-MM-dd"),

                    // Brokerage Details
                    BrokerageId = contract.BrokerageId,
                    OfgemId = contract.OfgemId,

                    // Dynamic Department-based fields
                    CloserId = contract.CloserId,
                    ReferralPartnerId = contract.ReferralPartnerId,
                    SubReferralPartnerId = contract.SubReferralPartnerId,
                    BrokerageStaffId = contract.BrokerageStaffId,
                    IntroducerId = contract.IntroducerId,
                    SubIntroducerId = contract.SubIntroducerId,
                    SubBrokerageId = contract.SubBrokerageId,
                    Collaboration = contract.Collaboration,
                    LeadGeneratorId = contract.LeadGeneratorId,

                    SupplierSnapshot = new GasSupplierSnapshotViewModel
                    {
                        Id = snapshot.Id,
                        SupplierId = snapshot.SupplierId,
                        EId = snapshot.EId,
                        SupplierName = snapshot.SupplierName,
                        Link = snapshot.SupplierLink,
                        Products = snapshot.CE_GasSupplierProductSnapshots.Select(p => new GasSupplierProductSnapshotViewModel
                        {
                            Id = p.Id,
                            ProductId = p.ProductId,
                            ProductName = p.ProductName,
                            SupplierCommsType = p.SupplierCommsType,
                            StartDate = p.StartDate.ToString("dd/MM/yyyy"),
                            EndDate = p.EndDate.ToString("dd/MM/yyyy"),
                            Commission = p.Commission
                        }).ToList(),
                        Uplifts = snapshot.CE_GasSupplierUpliftSnapshots.Select(u => new GasSupplierUpliftSnapshotViewModel
                        {
                            Id = u.Id,
                            Uplift = u.Uplift,
                            FuelType = u.FuelType,
                            StartDate = u.StartDate,
                            EndDate = u.EndDate
                        }).ToList(),
                        Contacts = snapshot.CE_GasSupplierContactSnapshots.Select(c => new GasSupplierContactSnapshotViewModel
                        {
                            Id = c.Id,
                            ContactName = c.ContactName,
                            Role = c.Role,
                            PhoneNumber = c.PhoneNumber,
                            Email = c.Email,
                            Notes = c.Notes
                        }).ToList()
                    }
                };

                return View("EditGas", model);
            }
            catch (Exception ex)
            {
                Logger.Log("Failed to Load Gas Contract : " + ex.Message);
                return JsonResponse.Fail("Failed to Load Gas Contract");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Pre-sales,Controls")]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> EditGas(GasContractEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .SelectMany(kvp => kvp.Value.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return JsonResponse.Fail(string.Join("<br>", errors));
            }

            // Validate contract lock before saving
            string currentUserId = User.Identity.GetUserId() ?? "Unknown";
            if (LockManager.Contracts.IsLocked(model.EId))
            {
                if (!LockManager.Contracts.IsLockedByUser(model.EId, currentUserId))
                {
                    string lockHolderUserId = LockManager.Contracts.GetLockHolder(model.EId);
                    string lockHolderUsername = await UserHelper.GetUsernameFromUserId(lockHolderUserId, HttpContext);
                    return JsonResponse.Fail($"Cannot save changes. This contract is currently being edited by {lockHolderUsername}.");
                }
            }
            else
            {
                // Contract is not locked at all - this shouldn't happen in normal flow
                return JsonResponse.Fail("Cannot save changes. Contract lock has expired. Please refresh the page and try again.");
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var contract = await db.CE_GasContracts.FirstOrDefaultAsync(c => c.EId == model.EId);
                    var account = await db.CE_Accounts.FirstOrDefaultAsync(a => a.EId == model.EId && a.Type == "Gas");

                    if (contract == null)
                        return JsonResponse.Fail("Contract not found.");

                    var snapshotProduct = await db.CE_GasSupplierProductSnapshots
                                        .FirstOrDefaultAsync(p => p.Id == model.ProductId);

                    if (snapshotProduct != null)
                    {
                        contract.ProductId = snapshotProduct.ProductId;
                    }
                    else
                    {
                        return JsonResponse.Fail("Selected product does not exist in snapshot.");
                    }

                    contract.MPRN = model.MPRN;
                    contract.BusinessName = model.BusinessName;
                    contract.CustomerName = model.CustomerName;
                    contract.BusinessDoorNumber = model.BusinessDoorNumber;
                    contract.BusinessHouseName = model.BusinessHouseName;
                    contract.BusinessStreet = model.BusinessStreet;
                    contract.BusinessTown = model.BusinessTown;
                    contract.BusinessCounty = model.BusinessCounty;
                    contract.PostCode = model.PostCode;
                    contract.PhoneNumber1 = model.PhoneNumber1;
                    contract.PhoneNumber2 = model.PhoneNumber2;
                    contract.EmailAddress = model.EmailAddress;
                    contract.InitialStartDate = model.InitialStartDate;
                    contract.Duration = model.Duration;
                    contract.Uplift = model.Uplift.ToString();
                    contract.InputEAC = model.InputEAC.ToString();
                    contract.UnitRate = model.UnitRate.ToString();
                    contract.OtherRate = model.OtherRate.ToString();
                    contract.StandingCharge = model.StandingCharge.ToString();
                    contract.CurrentSupplier = model.CurrentSupplier;
                    contract.SupplierId = model.SupplierId;
                   // contract.ProductId = model.ProductId;
                    contract.EMProcessor = model.EMProcessor;
                    contract.ContractChecked = model.ContractChecked;
                    contract.ContractAudited = model.ContractAudited;
                    contract.Terminated = model.Terminated;
                    contract.ContractNotes = model.ContractNotes;
                    contract.Department = model.Department;
                    contract.Source = model.Source;
                    contract.SalesType = model.SalesType;
                    contract.SalesTypeStatus = model.SalesTypeStatus;
                    contract.SupplierCommsType = model.SupplierCommsType;
                    contract.PreSalesStatus = model.PreSalesStatus;
                    contract.PreSalesFollowUpDate = DateTime.TryParse(model.PreSalesFollowUpDate, out DateTime presalesDate) ? presalesDate : (DateTime?)null;
                    contract.UpdatedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                    
                    // Update Brokerage Details
                    contract.BrokerageId = model.BrokerageId;
                    contract.OfgemId = model.OfgemId;
                    
                    // Update Dynamic Department-based fields
                    contract.CloserId = model.CloserId;
                    contract.ReferralPartnerId = model.ReferralPartnerId;
                    contract.SubReferralPartnerId = model.SubReferralPartnerId;
                    contract.BrokerageStaffId = model.BrokerageStaffId;
                    contract.IntroducerId = model.IntroducerId;
                    contract.SubIntroducerId = model.SubIntroducerId;
                    contract.SubBrokerageId = model.SubBrokerageId;
                    contract.Collaboration = model.Collaboration;
                    contract.LeadGeneratorId = model.LeadGeneratorId;

                    var triggeringStatuses = new[]
                    {
                        "Meter Registration Submitted", "New Connection Submitted", "Overturned Contract", "Submitted"
                    };

                    if (triggeringStatuses.Contains(model.PreSalesStatus))
                    {
                        contract.InputDate = DateTime.Now.ToString("dd/MM/yyyy");
                    }
                    model.InputDate = contract.InputDate;

                    if (account != null)
                    {
                        account.SortCode = model.SortCode;
                        account.AccountNumber = model.AccountNumber;
                    }

                    string username = model.EMProcessor ?? "PreSales Team";
                    string now = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                    db.CE_GasContractLogs.Add(new CE_GasContractLogs
                    {
                        EId = model.EId,
                        Username = username,
                        ActionDate = now,
                        PreSalesStatusType = model.PreSalesStatus,
                        Message = "Gas Contract updated."
                    });

                    if (model.ContractChecked)
                    {
                        db.CE_GasContractLogs.Add(new CE_GasContractLogs
                        {
                            EId = model.EId,
                            Username = username,
                            ActionDate = now,
                            PreSalesStatusType = model.PreSalesStatus,
                            Message = "Contract has been checked."
                        });
                    }

                    if (model.ContractAudited)
                    {
                        db.CE_GasContractLogs.Add(new CE_GasContractLogs
                        {
                            EId = model.EId,
                            Username = username,
                            ActionDate = now,
                            PreSalesStatusType = model.PreSalesStatus,
                            Message = "Contract has been audited."
                        });
                    }

                    if (model.Terminated)
                    {
                        db.CE_GasContractLogs.Add(new CE_GasContractLogs
                        {
                            EId = model.EId,
                            Username = username,
                            ActionDate = now,
                            PreSalesStatusType = model.PreSalesStatus,
                            Message = "Contract has been marked as terminated."
                        });
                    }

                    await db.SaveChangesAsync();
                    transaction.Commit();

                    return JsonResponse.Ok(new
                    {

                        contract.Department,
                        contract.Source,
                        contract.SalesType,
                        contract.SalesTypeStatus,
                        contract.BusinessName,
                        contract.CustomerName,
                        contract.BusinessDoorNumber,
                        contract.BusinessHouseName,
                        contract.BusinessStreet,
                        contract.BusinessTown,
                        contract.BusinessCounty,
                        contract.PostCode,
                        contract.PhoneNumber1,
                        contract.PhoneNumber2,
                        contract.EmailAddress,
                        contract.InitialStartDate,
                        contract.Duration,
                        contract.Uplift,
                        contract.InputEAC,
                        contract.UnitRate,
                        contract.OtherRate,
                        contract.StandingCharge,
                        contract.MPRN,
                        contract.CurrentSupplier,
                        contract.SupplierId,
                        contract.ProductId,
                        contract.SupplierCommsType,
                        contract.PreSalesStatus,
                        contract.ContractChecked,
                        contract.ContractAudited,
                        contract.Terminated,
                        contract.ContractNotes,
                        contract.EMProcessor,
                        contract.InputDate,
                        SortCode = account?.SortCode,
                        AccountNumber = account?.AccountNumber

                    }, "Gas contract updated successfully!");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Logger.Log("EditGas failed: " + ex.Message);
                    return JsonResponse.Fail("Error occurred while updating the gas contract.");
                }
            }
        }

        [HttpGet]
        [Authorize(Roles = "Pre-sales,Controls")]
        public async Task<JsonResult> GetLogsForGasContract(string eid)
        {
            try
            {
                var logs = await db.CE_GasContractLogs
                    .Where(l => l.EId == eid)
                    .Select(l => new
                    {
                        l.Username,
                        l.ActionDate,
                        l.PreSalesStatusType,
                        l.Message
                    })
                    .ToListAsync();

                var ordered = logs
                   .OrderByDescending(l => ParserHelper.ParseDateForSorting(l.ActionDate))
                   .Select(l => new
                   {
                       l.Username,
                       ActionDate = ParserHelper.FormatDateTimeForDisplay(l.ActionDate),
                       l.PreSalesStatusType,
                       l.Message
                   }).ToList();

                return JsonResponse.Ok(ordered);
            }
            catch (Exception ex)
            {
                Logger.Log("GetLogsForGasContract failed: " + ex.Message);
                return JsonResponse.Fail("Unable to retrieve logs.");
            }
        }

        #endregion

    }
}