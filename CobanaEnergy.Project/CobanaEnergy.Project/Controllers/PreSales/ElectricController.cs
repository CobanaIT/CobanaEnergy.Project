using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Filters;
using CobanaEnergy.Project.Helpers;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.AccountsDBModel;
using CobanaEnergy.Project.Models.Electric;
using CobanaEnergy.Project.Models.Electric.ElectricDBModels;
using CobanaEnergy.Project.Models.Electric.ElectricDBModels.snapshot;
using CobanaEnergy.Project.Models.Supplier.SupplierDBModels.snapshot;
using CobanaEnergy.Project.Models.Supplier.SupplierSnapshots;
using Logic;
using Logic.LockManager;
using Logic.ResponseModel.Helper;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Controllers.PreSales
{
    public class ElectricController : BaseController
    {
        private readonly ApplicationDBContext db;
        public ElectricController(ApplicationDBContext _db)
        {
            db = _db;
        }

        #region create_electric

        [HttpGet]
        [Authorize(Roles = "Pre-sales,Controls")]
        public async Task<ActionResult> CreateElectric()
        {
            return View("CreateElectric");
        }

        [HttpPost]
        [Authorize(Roles = "Pre-sales,Controls")]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> CreateElectric(ElectricContractCreateViewModel model)
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

                    var contract = new CE_ElectricContracts
                    {
                        EId = guid,
                        TopLine = model.TopLine,
                        MPAN = model.MPAN,
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
                        InputDate = DateTime.Now.ToString("dd/MM/yyyy"),//changed
                        InitialStartDate = model.InitialStartDate,
                        Uplift = model.Uplift.ToString(),
                        Duration = model.Duration,
                        InputEAC = model.InputEAC.ToString(),
                        DayRate = model.DayRate.ToString(),
                        NightRate = model.NightRate.ToString(),
                        EveWeekendRate = model.EveWeekendRate.ToString(),
                        OtherRate = model.OtherRate.ToString(),
                        StandingCharge = model.StandingCharge.ToString(),
                        //SortCode = model.SortCode,
                        //AccountNumber = model.AccountNumber,
                        CurrentSupplier = model.CurrentSupplier,
                        SupplierId = model.SupplierId,
                        ProductId = model.ProductId,
                        EMProcessor = model.EMProcessor,
                        ContractChecked = model.ContractChecked,
                        ContractAudited = model.ContractAudited,
                        Terminated = model.Terminated,
                        ContractNotes = model.ContractNotes,
                        //LOGS = model.LOGS,
                        CreatedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),//changed
                        UpdatedAt = "000",
                        Type = "Electric",
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

                    db.CE_ElectricContracts.Add(contract);
                    await db.SaveChangesAsync();

                    db.CE_Accounts.Add(new CE_Accounts
                    {
                        Type = "Electric",
                        EId = guid,
                        SortCode = model.SortCode,
                        AccountNumber = model.AccountNumber
                    });

                    string username = model.EMProcessor ?? "PreSales Team";
                    string now = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                    db.CE_ElectricContractLogs.Add(new CE_ElectricContractLogs
                    {
                        EId = guid,
                        Username = username,
                        ActionDate = now,
                        PreSalesStatusType = model.PreSalesStatus,
                        Message = "Electric Contract created."
                    });

                    if (model.ContractChecked)
                    {
                        db.CE_ElectricContractLogs.Add(new CE_ElectricContractLogs
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
                        db.CE_ElectricContractLogs.Add(new CE_ElectricContractLogs
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
                        db.CE_ElectricContractLogs.Add(new CE_ElectricContractLogs
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
                    var contractEId = guid;

                    var supplier = await db.CE_Supplier.FirstOrDefaultAsync(s => s.Id == supplierId);
                    if (supplier == null)
                    {
                        Logger.Log($"Supplier ID {supplierId} not found while creating snapshot.");
                        throw new Exception("Supplier not found for snapshot.");
                    }

                    var snapshot = new CE_ElectricSupplierSnapshots
                    {
                        EId = guid,
                        SupplierId = supplierId,
                        SupplierName = supplier.Name,
                        SupplierLink = supplier.Link,
                        CE_ElectricSupplierProductSnapshots = new List<CE_ElectricSupplierProductSnapshots>(),
                        CE_ElectricSupplierContactSnapshots = new List<CE_ElectricSupplierContactSnapshots>(),
                        CE_ElectricSupplierUpliftSnapshots = new List<CE_ElectricSupplierUpliftSnapshots>(),
                    };

                    var products = await db.CE_SupplierProducts
                        .Where(p => p.SupplierId == supplierId)
                        .ToListAsync();

                    foreach (var p in products)
                    {
                        snapshot.CE_ElectricSupplierProductSnapshots.Add(new CE_ElectricSupplierProductSnapshots
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
                        snapshot.CE_ElectricSupplierContactSnapshots.Add(new CE_ElectricSupplierContactSnapshots
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
                        .Where(u => u.SupplierId == supplierId && u.FuelType == "Electric")
                        .ToListAsync();

                    foreach (var u in uplifts)
                    {
                        snapshot.CE_ElectricSupplierUpliftSnapshots.Add(new CE_ElectricSupplierUpliftSnapshots
                        {
                            SupplierId = supplierId,
                            FuelType = u.FuelType,
                            Uplift = u.Uplift,
                            StartDate = u.StartDate,
                            EndDate = u.EndDate
                        });
                    }

                    db.CE_ElectricSupplierSnapshots.Add(snapshot);
                    await db.SaveChangesAsync();

                    transaction.Commit();
                    return JsonResponse.Ok(new { redirectUrl = Url.Action("Dashboard", "PreSales") }, "Electric contract created successfully!");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Logger.Log("CreateElectric failed: " + ex.Message);
                    return JsonResponse.Fail("An unexpected error occurred while saving the electric contract.");
                }
            }
        }

        #endregion

        #region CheckDuplicateMpan

        [HttpGet]
        [Authorize]
        public async Task<JsonResult> CheckDuplicateMpan(string mpan)
        {
            if (string.IsNullOrWhiteSpace(mpan) || mpan.Length != 13 || !mpan.All(char.IsDigit))
                return JsonResponse.Fail("Invalid MPAN format");

            try
            {
                var match = await db.CE_ElectricContracts
                    .Where(c => c.MPAN == mpan)
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
                Logger.Log("CheckDuplicateMpan failed: " + ex.Message);
                return JsonResponse.Fail("Error occurred while checking MPAN.");
            }
        }

        #endregion

        #region electric_edit

        [HttpGet]
        [Authorize(Roles = "Pre-sales,Controls")]
        public async Task<ActionResult> ElectricEdit(string eid)
        {
            if (string.IsNullOrWhiteSpace(eid))
                return HttpNotFound();
            try
            {
                var contract = await db.CE_ElectricContracts.FirstOrDefaultAsync(c => c.EId == eid);
                if (contract == null)
                    return HttpNotFound();

                var account = await db.CE_Accounts.FirstOrDefaultAsync(a => a.EId == eid && a.Type == "Electric");

                var snapshot = await db.CE_ElectricSupplierSnapshots
                    .Include(s => s.CE_ElectricSupplierProductSnapshots)
                    .Include(s => s.CE_ElectricSupplierContactSnapshots)
                    .Include(s => s.CE_ElectricSupplierUpliftSnapshots)
                    .FirstOrDefaultAsync(s => s.EId == contract.EId);

                if (snapshot == null)
                    return HttpNotFound();

                var savedProductSnapshot = snapshot.CE_ElectricSupplierProductSnapshots
                    .FirstOrDefault(p => p.ProductId == contract.ProductId);

                var model = new ElectricContractEditViewModel
                {
                    EId = contract.EId,
                    Department = contract.Department,
                    Source = contract.Source,
                    SalesType = contract.SalesType,
                    SalesTypeStatus = contract.SalesTypeStatus,
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
                    Duration = contract.Duration,
                    Uplift = Convert.ToDecimal(contract.Uplift),
                    InputEAC = Convert.ToDecimal(contract.InputEAC),
                    DayRate = Convert.ToDecimal(contract.DayRate),
                    NightRate = Convert.ToDecimal(contract.NightRate),
                    EveWeekendRate = Convert.ToDecimal(contract.EveWeekendRate),
                    OtherRate = Convert.ToDecimal(contract.OtherRate),
                    StandingCharge = Convert.ToDecimal(contract.StandingCharge),
                    SortCode = account?.SortCode,
                    AccountNumber = account?.AccountNumber,
                    TopLine = contract.TopLine,
                    MPAN = contract.MPAN,
                    CurrentSupplier = contract.CurrentSupplier,

                    SupplierId = snapshot.SupplierId,
                    ProductId = savedProductSnapshot?.Id ?? 0,
                    SupplierCommsType = savedProductSnapshot?.SupplierCommsType ?? contract.SupplierCommsType,

                    PreSalesStatus = contract.PreSalesStatus,
                    PreSalesFollowUpDate = contract.PreSalesFollowUpDate?.ToString("yyyy-MM-dd"),
                    EMProcessor = contract.EMProcessor,
                    ContractChecked = contract.ContractChecked,
                    ContractAudited = contract.ContractAudited,
                    Terminated = contract.Terminated,
                    ContractNotes = contract.ContractNotes,
                    InputDate = contract.InputDate,

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

                    SupplierSnapshot = new ElectricSupplierSnapshotViewModel
                    {
                        Id = snapshot.Id,
                        SupplierId = snapshot.SupplierId,
                        EId = snapshot.EId,
                        SupplierName = snapshot.SupplierName,
                        Link = snapshot.SupplierLink,
                        Products = snapshot.CE_ElectricSupplierProductSnapshots.Select(p => new ElectricSupplierProductSnapshotViewModel
                        {
                            Id = p.Id,
                            ProductId = p.ProductId,
                            ProductName = p.ProductName,
                            SupplierCommsType = p.SupplierCommsType,
                            StartDate = p.StartDate.ToString("dd/MM/yyyy"),
                            EndDate = p.EndDate.ToString("dd/MM/yyyy"),
                            Commission = p.Commission
                        }).ToList(),
                        Uplifts = snapshot.CE_ElectricSupplierUpliftSnapshots.Select(u => new ElectricSupplierUpliftSnapshotViewModel
                        {
                            Id = u.Id,
                            Uplift = u.Uplift,
                            FuelType = u.FuelType,
                            StartDate = u.StartDate,
                            EndDate = u.EndDate
                        }).ToList(),
                        Contacts = snapshot.CE_ElectricSupplierContactSnapshots.Select(c => new ElectricSupplierContactSnapshotViewModel
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

                return View("ElectricEdit", model);
            }
            catch (Exception ex)
            {
                Logger.Log("ElectricEdit failed: " + ex.ToString());
                return JsonResponse.Fail("An error occurred while fetching the contract.");
            }
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        [Authorize(Roles = "Pre-sales,Controls")]
        public async Task<JsonResult> ElectricEdit(ElectricContractEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(x => x.Errors).Select(e => e.ErrorMessage).ToList();
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
                    var contract = await db.CE_ElectricContracts.FirstOrDefaultAsync(c => c.EId == model.EId);
                    if (contract == null)
                        return JsonResponse.Fail("Electric Contract not found.");

                    contract.Department = model.Department;
                    contract.Source = model.Source;
                    contract.SalesType = model.SalesType;
                    contract.SalesTypeStatus = model.SalesTypeStatus;
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
                    contract.DayRate = model.DayRate.ToString();
                    contract.NightRate = model.NightRate.ToString();
                    contract.EveWeekendRate = model.EveWeekendRate.ToString();
                    contract.OtherRate = model.OtherRate.ToString();
                    contract.StandingCharge = model.StandingCharge.ToString();
                    contract.TopLine = model.TopLine;
                    contract.MPAN = model.MPAN;
                    contract.CurrentSupplier = model.CurrentSupplier;
                    contract.SupplierId = model.SupplierId;
                    var snapshotProduct = await db.CE_ElectricSupplierProductSnapshots
                                            .FirstOrDefaultAsync(p => p.Id == model.ProductId);

                    if (snapshotProduct != null)
                    {
                        contract.ProductId = snapshotProduct.ProductId;
                    }
                    else
                    {
                        return JsonResponse.Fail("Selected product does not exist in snapshot.");
                    }
                   // contract.ProductId = model.ProductId;
                    contract.SupplierCommsType = model.SupplierCommsType;
                    contract.PreSalesStatus = model.PreSalesStatus;
                    contract.PreSalesFollowUpDate = DateTime.TryParse(model.PreSalesFollowUpDate, out DateTime presalesDate) ? presalesDate : (DateTime?)null;
                    contract.ContractChecked = model.ContractChecked;
                    contract.ContractAudited = model.ContractAudited;
                    contract.Terminated = model.Terminated;
                    contract.ContractNotes = model.ContractNotes;
                    contract.UpdatedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                    contract.EMProcessor = model.EMProcessor;

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

                    var account = await db.CE_Accounts.FirstOrDefaultAsync(a => a.EId == model.EId && a.Type == "Electric");
                    if (account != null)
                    {
                        account.SortCode = model.SortCode;
                        account.AccountNumber = model.AccountNumber;
                    }

                    string username = model.EMProcessor ?? "PreSales Team";
                    string now = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                    db.CE_ElectricContractLogs.Add(new CE_ElectricContractLogs
                    {
                        EId = model.EId,
                        Username = username,
                        ActionDate = now,
                        PreSalesStatusType = model.PreSalesStatus,
                        Message = "Electric contract updated."
                    });

                    if (model.ContractChecked)
                    {
                        db.CE_ElectricContractLogs.Add(new CE_ElectricContractLogs
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
                        db.CE_ElectricContractLogs.Add(new CE_ElectricContractLogs
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
                        db.CE_ElectricContractLogs.Add(new CE_ElectricContractLogs
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
                        contract.DayRate,
                        contract.NightRate,
                        contract.EveWeekendRate,
                        contract.OtherRate,
                        contract.StandingCharge,
                        contract.TopLine,
                        contract.MPAN,
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
                    }, "Electric contract updated successfully!");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Logger.Log("ElectricEdit failed: " + ex.ToString());
                    return JsonResponse.Fail("An error occurred while updating the contract.");
                }
            }
        }

        [HttpGet]
        [Authorize(Roles = "Pre-sales,Controls")]
        public async Task<JsonResult> GetLogsForElectricContract(string eid)
        {
            if (string.IsNullOrWhiteSpace(eid))
                return JsonResponse.Fail("Invalid Id.");

            try
            {
                var logs = await db.CE_ElectricContractLogs
                    .Where(l => l.EId == eid)
                    .Select(l => new
                    {
                        l.Username,
                        l.ActionDate,
                        l.PreSalesStatusType,
                        l.Message
                    }).ToListAsync();

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
                Logger.Log("Error fetching logs: " + ex.ToString());
                return JsonResponse.Fail("Failed to fetch logs.");
            }
        }

        #endregion
    }
}