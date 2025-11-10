using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Filters;
using CobanaEnergy.Project.Helpers;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.AccountsDBModel;
using CobanaEnergy.Project.Models.Business;
using CobanaEnergy.Project.Models.Dual;
using CobanaEnergy.Project.Models.Dual.EditDual;
using CobanaEnergy.Project.Models.Dual.LogsHelperClass;
using CobanaEnergy.Project.Models.Electric.ElectricDBModels;
using CobanaEnergy.Project.Models.Electric.ElectricDBModels.snapshot;
using CobanaEnergy.Project.Models.Gas.GasDBModels;
using CobanaEnergy.Project.Models.Supplier.SupplierDBModels.snapshot;
using CobanaEnergy.Project.Models.Supplier.SupplierDBModels.snapshot_Gas;
using CobanaEnergy.Project.Models.Supplier.SupplierSnapshots;
using CobanaEnergy.Project.Models.Supplier.SupplierSnapshots_Gas;
using Logic;
using Logic.LockManager;
using Logic.ResponseModel.Helper;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Controllers.PreSales
{
    public class DualController : BaseController
    {
        private readonly ApplicationDBContext _db;
        public DualController(ApplicationDBContext db)
        {
            _db = db;
        }

        #region Dual_Contract

        [HttpGet]
        [Authorize(Roles = "Pre-sales,Controls")]
        public async Task<ActionResult> CreateDual()
        {
            return View("CreateDual");
        }

        [HttpPost]
        [Authorize(Roles = "Pre-sales,Controls")]
        [ValidateJsonAntiForgeryToken]
        public async Task<ActionResult> CreateDual(DualContractCreateViewModel model)
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

            using (var tx = _db.Database.BeginTransaction())
            {
                try
                {
                    var guid = Guid.NewGuid().ToString().ToUpper();

                    var electric = new CE_ElectricContracts
                    {
                        EId = guid,
                        Department = model.Department,
                        Source = model.Source,
                        SalesType = model.ElectricSalesType,
                        SalesTypeStatus = model.ElectricSalesTypeStatus,
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
                        InitialStartDate = model.ElectricInitialStartDate,
                        Duration = model.ElectricDuration,
                        Uplift = model.ElectricUplift.ToString(),
                        InputEAC = model.ElectricInputEAC.ToString(),
                        DayRate = model.ElectricDayRate.ToString(),
                        NightRate = model.ElectricNightRate.ToString(),
                        EveWeekendRate = model.ElectricEveWeekendRate.ToString(),
                        OtherRate = model.ElectricOtherRate.ToString(),
                        StandingCharge = model.ElectricStandingCharge.ToString(),
                        TopLine = model.TopLine,
                        MPAN = model.MPAN,
                        CurrentSupplier = model.ElectricCurrentSupplier,
                        SupplierId = model.ElectricSupplierId,
                        ProductId = model.ElectricProductId,
                        SupplierCommsType = model.ElectricSupplierCommsType,
                        PreSalesStatus = model.ElectricPreSalesStatus,
                        PreSalesFollowUpDate = DateTime.TryParse(model.ElectricPreSalesFollowUpDate, out DateTime electricPresalesDate) ? electricPresalesDate : (DateTime?)null,
                        EMProcessor = model.EMProcessor,
                        ContractChecked = model.ContractChecked,
                        ContractAudited = model.ContractAudited,
                        Terminated = model.Terminated,
                        ContractNotes = model.ContractNotes,
                        CreatedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                        UpdatedAt = "000",
                        Type = "Dual",
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

                    _db.CE_ElectricContracts.Add(electric);
                    await _db.SaveChangesAsync();

                    string username = model.EMProcessor ?? "PreSales Team";
                    string now = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                    _db.CE_ElectricContractLogs.Add(new CE_ElectricContractLogs
                    {
                        EId = guid,
                        Username = username,
                        ActionDate = now,
                        PreSalesStatusType = model.ElectricPreSalesStatus,
                        Message = "Dual Contract created."
                    });

                    if (model.ContractChecked)
                    {
                        _db.CE_ElectricContractLogs.Add(new CE_ElectricContractLogs
                        {
                            EId = guid,
                            Username = username,
                            ActionDate = now,
                            PreSalesStatusType = model.ElectricPreSalesStatus,
                            Message = "Contract has been checked."
                        });
                    }

                    if (model.ContractAudited)
                    {
                        _db.CE_ElectricContractLogs.Add(new CE_ElectricContractLogs
                        {
                            EId = guid,
                            Username = username,
                            ActionDate = now,
                            PreSalesStatusType = model.ElectricPreSalesStatus,
                            Message = "Contract has been audited."
                        });
                    }

                    if (model.Terminated)
                    {
                        _db.CE_ElectricContractLogs.Add(new CE_ElectricContractLogs
                        {
                            EId = guid,
                            Username = username,
                            ActionDate = now,
                            PreSalesStatusType = model.ElectricPreSalesStatus,
                            Message = "Contract has been marked as terminated."
                        });
                    }

                    // ---- ELECTRIC SNAPSHOT ----
                    var electricSupplier = await _db.CE_Supplier
                        .FirstOrDefaultAsync(s => s.Id == model.ElectricSupplierId);

                    if (electricSupplier == null)
                    {
                        Logger.Log($"Electric Supplier ID {model.ElectricSupplierId} not found while creating snapshot.");
                        throw new Exception("Electric Supplier not found for snapshot.");
                    }

                    var electricSnapshot = new CE_ElectricSupplierSnapshots
                    {
                        EId = guid,
                        SupplierId = electricSupplier.Id,
                        SupplierName = electricSupplier.Name,
                        SupplierLink = electricSupplier.Link,
                        CE_ElectricSupplierProductSnapshots = new List<CE_ElectricSupplierProductSnapshots>(),
                        CE_ElectricSupplierContactSnapshots = new List<CE_ElectricSupplierContactSnapshots>(),
                        CE_ElectricSupplierUpliftSnapshots = new List<CE_ElectricSupplierUpliftSnapshots>()
                    };

                    var electricProducts = await _db.CE_SupplierProducts
                        .Where(p => p.SupplierId == electricSupplier.Id)
                        .ToListAsync();

                    foreach (var product in electricProducts)
                    {
                        electricSnapshot.CE_ElectricSupplierProductSnapshots.Add(new CE_ElectricSupplierProductSnapshots
                        {
                            SupplierId = electricSupplier.Id,
                            ProductId = product.Id,
                            ProductName = product.ProductName,
                            StartDate = DateTime.Parse(product.StartDate),
                            EndDate = DateTime.Parse(product.EndDate),
                            Commission = product.Commission,
                            SupplierCommsType = product.SupplierCommsType
                        });
                    }

                    var electricContacts = await _db.CE_SupplierContacts
                        .Where(c => c.SupplierId == electricSupplier.Id)
                        .ToListAsync();

                    foreach (var contact in electricContacts)
                    {
                        electricSnapshot.CE_ElectricSupplierContactSnapshots.Add(new CE_ElectricSupplierContactSnapshots
                        {
                            SupplierId = electricSupplier.Id,
                            ContactName = contact.ContactName,
                            Role = contact.Role,
                            PhoneNumber = contact.PhoneNumber,
                            Email = contact.Email,
                            Notes = contact.Notes
                        });
                    }

                    var electricUplifts = await _db.CE_SupplierUplifts
                        .Where(u => u.SupplierId == electricSupplier.Id && u.FuelType == "Electric")
                        .ToListAsync();

                    foreach (var uplift in electricUplifts)
                    {
                        electricSnapshot.CE_ElectricSupplierUpliftSnapshots.Add(new CE_ElectricSupplierUpliftSnapshots
                        {
                            SupplierId = electricSupplier.Id,
                            FuelType = uplift.FuelType,
                            Uplift = uplift.Uplift,
                            StartDate = uplift.StartDate,
                            EndDate = uplift.EndDate
                        });
                    }

                    _db.CE_ElectricSupplierSnapshots.Add(electricSnapshot);
                    await _db.SaveChangesAsync();

                    var gas = new CE_GasContracts
                    {
                        EId = guid,
                        Department = model.Department,
                        Source = model.Source,
                        SalesType = model.GasSalesType,
                        SalesTypeStatus = model.GasSalesTypeStatus,
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
                        InitialStartDate = model.GasInitialStartDate,
                        Duration = model.GasDuration,
                        Uplift = model.GasUplift.ToString(),
                        InputEAC = model.GasInputEAC.ToString(),
                        UnitRate = model.GasUnitRate.ToString(),
                        OtherRate = model.GasOtherRate.ToString(),
                        StandingCharge = model.GasStandingCharge.ToString(),
                        MPRN = model.MPRN,
                        CurrentSupplier = model.GasCurrentSupplier,
                        SupplierId = model.GasSupplierId,
                        ProductId = model.GasProductId,
                        SupplierCommsType = model.GasSupplierCommsType,
                        PreSalesStatus = model.GasPreSalesStatus,
                        PreSalesFollowUpDate = DateTime.TryParse(model.GasPreSalesFollowUpDate, out DateTime gasPresalesDate) ? gasPresalesDate : (DateTime?)null,
                        EMProcessor = model.EMProcessor,
                        ContractChecked = model.ContractChecked,
                        ContractAudited = model.ContractAudited,
                        Terminated = model.Terminated,
                        ContractNotes = model.ContractNotes,
                        CreatedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                        UpdatedAt = "000",
                        Type = "Dual",
                        
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

                    _db.CE_GasContracts.Add(gas);
                    await _db.SaveChangesAsync();

                    _db.CE_GasContractLogs.Add(new CE_GasContractLogs
                    {
                        EId = guid,
                        Username = username,
                        ActionDate = now,
                        PreSalesStatusType = model.GasPreSalesStatus,
                        Message = "Dual Contract created."
                    });

                    if (model.ContractChecked)
                    {
                        _db.CE_GasContractLogs.Add(new CE_GasContractLogs
                        {
                            EId = guid,
                            Username = username,
                            ActionDate = now,
                            PreSalesStatusType = model.GasPreSalesStatus,
                            Message = "Contract has been checked."
                        });
                    }

                    if (model.ContractAudited)
                    {
                        _db.CE_GasContractLogs.Add(new CE_GasContractLogs
                        {
                            EId = guid,
                            Username = username,
                            ActionDate = now,
                            PreSalesStatusType = model.GasPreSalesStatus,
                            Message = "Contract has been audited."
                        });
                    }

                    if (model.Terminated)
                    {
                        _db.CE_GasContractLogs.Add(new CE_GasContractLogs
                        {
                            EId = guid,
                            Username = username,
                            ActionDate = now,
                            PreSalesStatusType = model.GasPreSalesStatus,
                            Message = "Contract has been marked as terminated."
                        });
                    }

                    _db.CE_Accounts.Add(new CE_Accounts
                    {
                        Type = "Dual",
                        EId = guid,
                        SortCode = model.SortCode,
                        AccountNumber = model.AccountNumber
                    });

                    await _db.SaveChangesAsync();

                    // ---- GAS SNAPSHOT ----
                    var gasSupplier = await _db.CE_Supplier
                        .FirstOrDefaultAsync(s => s.Id == model.GasSupplierId);

                    if (gasSupplier == null)
                    {
                        Logger.Log($"Gas Supplier ID {model.GasSupplierId} not found while creating snapshot.");
                        throw new Exception("Gas Supplier not found for snapshot.");
                    }

                    var gasSnapshot = new CE_GasSupplierSnapshots
                    {
                        EId = guid,
                        SupplierId = gasSupplier.Id,
                        SupplierName = gasSupplier.Name,
                        SupplierLink = gasSupplier.Link,
                        CE_GasSupplierProductSnapshots = new List<CE_GasSupplierProductSnapshots>(),
                        CE_GasSupplierContactSnapshots = new List<CE_GasSupplierContactSnapshots>(),
                        CE_GasSupplierUpliftSnapshots = new List<CE_GasSupplierUpliftSnapshots>()
                    };

                    var gasProducts = await _db.CE_SupplierProducts
                        .Where(p => p.SupplierId == gasSupplier.Id)
                        .ToListAsync();

                    foreach (var product in gasProducts)
                    {
                        gasSnapshot.CE_GasSupplierProductSnapshots.Add(new CE_GasSupplierProductSnapshots
                        {
                            SupplierId = gasSupplier.Id,
                            ProductId = product.Id,
                            ProductName = product.ProductName,
                            StartDate = DateTime.Parse(product.StartDate),
                            EndDate = DateTime.Parse(product.EndDate),
                            Commission = product.Commission,
                            SupplierCommsType = product.SupplierCommsType
                        });
                    }

                    var gasContacts = await _db.CE_SupplierContacts
                        .Where(c => c.SupplierId == gasSupplier.Id)
                        .ToListAsync();

                    foreach (var contact in gasContacts)
                    {
                        gasSnapshot.CE_GasSupplierContactSnapshots.Add(new CE_GasSupplierContactSnapshots
                        {
                            SupplierId = gasSupplier.Id,
                            ContactName = contact.ContactName,
                            Role = contact.Role,
                            PhoneNumber = contact.PhoneNumber,
                            Email = contact.Email,
                            Notes = contact.Notes
                        });
                    }

                    var gasUplifts = await _db.CE_SupplierUplifts
                        .Where(u => u.SupplierId == gasSupplier.Id && u.FuelType == "Gas")
                        .ToListAsync();

                    foreach (var uplift in gasUplifts)
                    {
                        gasSnapshot.CE_GasSupplierUpliftSnapshots.Add(new CE_GasSupplierUpliftSnapshots
                        {
                            SupplierId = gasSupplier.Id,
                            FuelType = uplift.FuelType,
                            Uplift = uplift.Uplift,
                            StartDate = uplift.StartDate,
                            EndDate = uplift.EndDate
                        });
                    }

                    _db.CE_GasSupplierSnapshots.Add(gasSnapshot);
                    await _db.SaveChangesAsync();


                    // Add Business Contact Info

                    _db.CE_BusinessContactInfos.Add(new CE_BusinessContactInfo
                    {
                        EId = electric.EId,
                        Type = electric.Type,
                        BusinessName = electric.BusinessName,
                        CustomerName = electric.CustomerName,
                        PhoneNumber1 = electric.PhoneNumber1,
                        PhoneNumber2 = electric.PhoneNumber2,
                        EmailAddress = electric.EmailAddress

                    });

                    await _db.SaveChangesAsync();


                    tx.Commit();

                    return JsonResponse.Ok(new { redirectUrl = Url.Action("Dashboard", "PreSales") }, "Dual contract created successfully.");
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    Logger.Log("DualController.CreateDual: " + ex);
                    return JsonResponse.Fail("An unexpected error occurred while creating the dual contract.");
                }
            }
        }

        #endregion

        #region Edit_Dual

        [HttpGet]
        [Authorize(Roles = "Pre-sales,Controls")]
        public async Task<ActionResult> EditDual(string eid)
        {
            if (string.IsNullOrWhiteSpace(eid))
                return HttpNotFound();

            try
            {
                var electric = await _db.CE_ElectricContracts.FirstOrDefaultAsync(e => e.EId == eid && e.Type == "Dual");
                var gas = await _db.CE_GasContracts.FirstOrDefaultAsync(g => g.EId == eid && g.Type == "Dual");
                var account = await _db.CE_Accounts.FirstOrDefaultAsync(a => a.EId == eid && a.Type == "Dual");

                if (electric == null || gas == null)
                    return HttpNotFound();

                // Load Electric Snapshot
                var electricSnapshot = await _db.CE_ElectricSupplierSnapshots
                    .Include(s => s.CE_ElectricSupplierProductSnapshots)
                    .Include(s => s.CE_ElectricSupplierContactSnapshots)
                    .Include(s => s.CE_ElectricSupplierUpliftSnapshots)
                    .FirstOrDefaultAsync(s => s.EId == electric.EId);

                if (electricSnapshot == null)
                    return HttpNotFound();

                var savedElectricProductSnapshot = electricSnapshot.CE_ElectricSupplierProductSnapshots
                    .FirstOrDefault(p => p.ProductId == electric.ProductId);

                // Load Gas Snapshot
                var gasSnapshot = await _db.CE_GasSupplierSnapshots
                    .Include(s => s.CE_GasSupplierProductSnapshots)
                    .Include(s => s.CE_GasSupplierContactSnapshots)
                    .Include(s => s.CE_GasSupplierUpliftSnapshots)
                    .FirstOrDefaultAsync(s => s.EId == gas.EId);

                if (gasSnapshot == null)
                    return HttpNotFound();

                var savedGasProductSnapshot = gasSnapshot.CE_GasSupplierProductSnapshots
                    .FirstOrDefault(p => p.ProductId == gas.ProductId);

                var model = new DualContractEditViewModel
                {
                    EId = eid,

                    // Shared
                    Department = electric.Department,
                    Source = electric.Source,
                    BusinessName = electric.BusinessName,
                    CustomerName = electric.CustomerName,
                    BusinessDoorNumber = electric.BusinessDoorNumber,
                    BusinessHouseName = electric.BusinessHouseName,
                    BusinessStreet = electric.BusinessStreet,
                    BusinessTown = electric.BusinessTown,
                    BusinessCounty = electric.BusinessCounty,
                    PostCode = electric.PostCode,
                    PhoneNumber1 = electric.PhoneNumber1,
                    PhoneNumber2 = electric.PhoneNumber2,
                    EmailAddress = electric.EmailAddress,
                    EMProcessor = electric.EMProcessor,
                    ContractChecked = electric.ContractChecked,
                    ContractAudited = electric.ContractAudited,
                    Terminated = electric.Terminated,
                    ContractNotes = electric.ContractNotes,
                    SortCode = account?.SortCode ?? "",
                    AccountNumber = account?.AccountNumber ?? "",
                    ElectricInputDate = electric.InputDate,

                    // Electric
                    TopLine = electric.TopLine,
                    MPAN = electric.MPAN,
                    ElectricCurrentSupplier = electric.CurrentSupplier,
                    ElectricSupplierId = electricSnapshot.SupplierId,
                    ElectricProductId = savedElectricProductSnapshot?.Id ?? 0,
                    ElectricInitialStartDate = electric.InitialStartDate,
                    ElectricDuration = electric.Duration,
                    ElectricSalesType = electric.SalesType,
                    ElectricSalesTypeStatus = electric.SalesTypeStatus,
                    ElectricUplift = Convert.ToDecimal(electric.Uplift),
                    ElectricInputEAC = Convert.ToDecimal(electric.InputEAC),
                    ElectricDayRate = Convert.ToDecimal(electric.DayRate),
                    ElectricNightRate = Convert.ToDecimal(electric.NightRate),
                    ElectricEveWeekendRate = Convert.ToDecimal(electric.EveWeekendRate),
                    ElectricOtherRate = Convert.ToDecimal(electric.OtherRate),
                    ElectricStandingCharge = Convert.ToDecimal(electric.StandingCharge),
                    ElectricSupplierCommsType = savedElectricProductSnapshot?.SupplierCommsType ?? electric.SupplierCommsType,
                    ElectricPreSalesStatus = electric.PreSalesStatus,
                    ElectricPreSalesFollowUpDate = electric.PreSalesFollowUpDate?.ToString("yyyy-MM-dd"),

                    // Electric Snapshot
                    ElectricSupplierSnapshot = new ElectricSupplierSnapshotViewModel
                    {
                        Id = electricSnapshot.Id,
                        SupplierId = electricSnapshot.SupplierId,
                        EId = electricSnapshot.EId,
                        SupplierName = electricSnapshot.SupplierName,
                        Link = electricSnapshot.SupplierLink,
                        Products = electricSnapshot.CE_ElectricSupplierProductSnapshots.Select(p => new ElectricSupplierProductSnapshotViewModel
                        {
                            Id = p.Id,
                            ProductId = p.ProductId,
                            ProductName = p.ProductName,
                            SupplierCommsType = p.SupplierCommsType,
                            StartDate = p.StartDate.ToString("dd/MM/yyyy"),
                            EndDate = p.EndDate.ToString("dd/MM/yyyy"),
                            Commission = p.Commission
                        }).ToList(),
                        Uplifts = electricSnapshot.CE_ElectricSupplierUpliftSnapshots.Select(u => new ElectricSupplierUpliftSnapshotViewModel
                        {
                            Id = u.Id,
                            Uplift = u.Uplift,
                            FuelType = u.FuelType,
                            StartDate = u.StartDate,
                            EndDate = u.EndDate
                        }).ToList(),
                        Contacts = electricSnapshot.CE_ElectricSupplierContactSnapshots.Select(c => new ElectricSupplierContactSnapshotViewModel
                        {
                            Id = c.Id,
                            ContactName = c.ContactName,
                            Role = c.Role,
                            PhoneNumber = c.PhoneNumber,
                            Email = c.Email,
                            Notes = c.Notes
                        }).ToList()
                    },

                    // Gas
                    MPRN = gas.MPRN,
                    GasInputDate = gas.InputDate,
                    GasCurrentSupplier = gas.CurrentSupplier,
                    GasSupplierId = gasSnapshot.SupplierId,
                    GasProductId = savedGasProductSnapshot?.Id ?? 0,
                    GasInitialStartDate = gas.InitialStartDate,
                    GasDuration = gas.Duration,
                    GasSalesType = gas.SalesType,
                    GasSalesTypeStatus = gas.SalesTypeStatus,
                    GasUplift = Convert.ToDecimal(gas.Uplift),
                    GasInputEAC = Convert.ToDecimal(gas.InputEAC),
                    GasUnitRate = Convert.ToDecimal(gas.UnitRate),
                    GasOtherRate = Convert.ToDecimal(gas.OtherRate),
                    GasStandingCharge = Convert.ToDecimal(gas.StandingCharge),
                    GasSupplierCommsType = savedGasProductSnapshot?.SupplierCommsType ?? gas.SupplierCommsType,
                    GasPreSalesStatus = gas.PreSalesStatus,
                    GasPreSalesFollowUpDate = gas.PreSalesFollowUpDate?.ToString("yyyy-MM-dd"),
                    // Gas Snapshot
                    GasSupplierSnapshot = new GasSupplierSnapshotViewModel
                    {
                        Id = gasSnapshot.Id,
                        SupplierId = gasSnapshot.SupplierId,
                        EId = gasSnapshot.EId,
                        SupplierName = gasSnapshot.SupplierName,
                        Link = gasSnapshot.SupplierLink,
                        Products = gasSnapshot.CE_GasSupplierProductSnapshots.Select(p => new GasSupplierProductSnapshotViewModel
                        {
                            Id = p.Id,
                            ProductId = p.ProductId,
                            ProductName = p.ProductName,
                            SupplierCommsType = p.SupplierCommsType,
                            StartDate = p.StartDate.ToString("dd/MM/yyyy"),
                            EndDate = p.EndDate.ToString("dd/MM/yyyy"),
                            Commission = p.Commission
                        }).ToList(),
                        Uplifts = gasSnapshot.CE_GasSupplierUpliftSnapshots.Select(u => new GasSupplierUpliftSnapshotViewModel
                        {
                            Id = u.Id,
                            Uplift = u.Uplift,
                            FuelType = u.FuelType,
                            StartDate = u.StartDate,
                            EndDate = u.EndDate
                        }).ToList(),
                        Contacts = gasSnapshot.CE_GasSupplierContactSnapshots.Select(c => new GasSupplierContactSnapshotViewModel
                        {
                            Id = c.Id,
                            ContactName = c.ContactName,
                            Role = c.Role,
                            PhoneNumber = c.PhoneNumber,
                            Email = c.Email,
                            Notes = c.Notes
                        }).ToList()
                    },
                    // Brokerage Details
                    BrokerageId = electric.BrokerageId,
                    OfgemId = electric.OfgemId,

                    // Dynamic Department-based fields
                    CloserId = electric.CloserId,
                    ReferralPartnerId = electric.ReferralPartnerId,
                    SubReferralPartnerId = electric.SubReferralPartnerId,
                    BrokerageStaffId = electric.BrokerageStaffId,
                    IntroducerId = electric.IntroducerId,
                    SubIntroducerId = electric.SubIntroducerId,
                    SubBrokerageId = electric.SubBrokerageId,
                    Collaboration = electric.Collaboration,
                    LeadGeneratorId = electric.LeadGeneratorId
                };

                return View("EditDual", model);
            }
            catch (Exception ex)
            {
                Logger.Log("EditDual failed to load: " + ex.Message);
                return JsonResponse.Fail("Failed to load dual contract.");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Pre-sales,Controls")]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> EditDual(DualContractEditViewModel model)
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

            using (var tx = _db.Database.BeginTransaction())
            {
                try
                {
                    var electric = await _db.CE_ElectricContracts.FirstOrDefaultAsync(e => e.EId == model.EId && e.Type == "Dual");
                    var gas = await _db.CE_GasContracts.FirstOrDefaultAsync(g => g.EId == model.EId && g.Type == "Dual");
                    var account = await _db.CE_Accounts.FirstOrDefaultAsync(a => a.EId == model.EId && a.Type == "Dual");

                    if (electric == null || gas == null)
                        return JsonResponse.Fail("Dual contract not found.");

                    var username = model.EMProcessor ?? "PreSales Team";
                    var now = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

                    void Log(object logCollection, string status, string msg)
                    {
                        if (logCollection is DbSet<CE_ElectricContractLogs> elogs)
                            elogs.Add(new CE_ElectricContractLogs { EId = model.EId, Username = username, ActionDate = now, PreSalesStatusType = status, Message = msg });
                        if (logCollection is DbSet<CE_GasContractLogs> glogs)
                            glogs.Add(new CE_GasContractLogs { EId = model.EId, Username = username, ActionDate = now, PreSalesStatusType = status, Message = msg });
                    }

                    // ---- ELECTRIC
                    electric.Department = model.Department;
                    electric.Source = model.Source;
                    electric.TopLine = model.TopLine;
                    electric.MPAN = model.MPAN;
                    electric.CurrentSupplier = model.ElectricCurrentSupplier;
                    electric.SupplierId = model.ElectricSupplierId;
                    var electricSnapshotProduct = await _db.CE_ElectricSupplierProductSnapshots
                                                    .FirstOrDefaultAsync(p => p.Id == model.ElectricProductId);

                    if (electricSnapshotProduct != null)
                    {
                        electric.ProductId = electricSnapshotProduct.ProductId;
                    }
                    else
                    {
                        return JsonResponse.Fail("Selected Electric product does not exist in snapshot.");
                    }
                    // electric.ProductId = model.ElectricProductId;
                    electric.SupplierCommsType = model.ElectricSupplierCommsType;
                    electric.SalesType = model.ElectricSalesType;
                    electric.SalesTypeStatus = model.ElectricSalesTypeStatus;
                    electric.InitialStartDate = model.ElectricInitialStartDate;
                    electric.Duration = model.ElectricDuration;
                    electric.Uplift = model.ElectricUplift.ToString();
                    electric.InputEAC = model.ElectricInputEAC.ToString();
                    electric.DayRate = model.ElectricDayRate.ToString();
                    electric.NightRate = model.ElectricNightRate.ToString();
                    electric.EveWeekendRate = model.ElectricEveWeekendRate.ToString();
                    electric.OtherRate = model.ElectricOtherRate.ToString();
                    electric.StandingCharge = model.ElectricStandingCharge.ToString();
                    electric.BusinessName = model.BusinessName;
                    electric.CustomerName = model.CustomerName;
                    electric.BusinessDoorNumber = model.BusinessDoorNumber;
                    electric.BusinessHouseName = model.BusinessHouseName;
                    electric.BusinessStreet = model.BusinessStreet;
                    electric.BusinessTown = model.BusinessTown;
                    electric.BusinessCounty = model.BusinessCounty;
                    electric.PostCode = model.PostCode;
                    electric.PhoneNumber1 = model.PhoneNumber1;
                    electric.PhoneNumber2 = model.PhoneNumber2;
                    electric.EmailAddress = model.EmailAddress;
                    electric.PreSalesStatus = model.ElectricPreSalesStatus;
                    electric.PreSalesFollowUpDate = DateTime.TryParse(model.ElectricPreSalesFollowUpDate, out DateTime electricPresalesDate) ? electricPresalesDate : (DateTime?)null;
                    electric.EMProcessor = model.EMProcessor;
                    electric.ContractChecked = model.ContractChecked;
                    electric.ContractAudited = model.ContractAudited;
                    electric.Terminated = model.Terminated;
                    electric.ContractNotes = model.ContractNotes;
                    electric.UpdatedAt = now;

                    // Update Electric contract dynamic fields (Electric-first priority for common fields)
                    electric.BrokerageId = model.BrokerageId;
                    electric.OfgemId = model.OfgemId;
                    electric.CloserId = model.CloserId;
                    electric.ReferralPartnerId = model.ReferralPartnerId;
                    electric.SubReferralPartnerId = model.SubReferralPartnerId;
                    electric.BrokerageStaffId = model.BrokerageStaffId;
                    electric.IntroducerId = model.IntroducerId;
                    electric.SubIntroducerId = model.SubIntroducerId;
                    electric.SubBrokerageId = model.SubBrokerageId;
                    electric.Collaboration = model.Collaboration;
                    electric.LeadGeneratorId = model.LeadGeneratorId;

                    // InputDate trigger
                    var triggeringStatuses = new[] { "Meter Registration Submitted", "New Connection Submitted", "Overturned Contract", "Submitted" };
                    if (triggeringStatuses.Contains(model.ElectricPreSalesStatus))
                        electric.InputDate = DateTime.Now.ToString("dd/MM/yyyy");

                    // ---- GAS
                    gas.Department = model.Department;
                    gas.Source = model.Source;
                    gas.MPRN = model.MPRN;
                    gas.CurrentSupplier = model.GasCurrentSupplier;
                    gas.SupplierId = model.GasSupplierId;
                    var gasSnapshotProduct = await _db.CE_GasSupplierProductSnapshots
                                            .FirstOrDefaultAsync(p => p.Id == model.GasProductId);

                    if (gasSnapshotProduct != null)
                    {
                        gas.ProductId = gasSnapshotProduct.ProductId;
                    }
                    else
                    {
                        return JsonResponse.Fail("Selected Gas product does not exist in snapshot.");
                    }
                    // gas.ProductId = model.GasProductId;
                    gas.SupplierCommsType = model.GasSupplierCommsType;
                    gas.SalesType = model.GasSalesType;
                    gas.SalesTypeStatus = model.GasSalesTypeStatus;
                    gas.InitialStartDate = model.GasInitialStartDate;
                    gas.Duration = model.GasDuration;
                    gas.Uplift = model.GasUplift.ToString();
                    gas.InputEAC = model.GasInputEAC.ToString();
                    gas.UnitRate = model.GasUnitRate.ToString();
                    gas.OtherRate = model.GasOtherRate.ToString();
                    gas.StandingCharge = model.GasStandingCharge.ToString();
                    gas.BusinessName = model.BusinessName;
                    gas.CustomerName = model.CustomerName;
                    gas.BusinessDoorNumber = model.BusinessDoorNumber;
                    gas.BusinessHouseName = model.BusinessHouseName;
                    gas.BusinessStreet = model.BusinessStreet;
                    gas.BusinessTown = model.BusinessTown;
                    gas.BusinessCounty = model.BusinessCounty;
                    gas.PostCode = model.PostCode;
                    gas.PhoneNumber1 = model.PhoneNumber1;
                    gas.PhoneNumber2 = model.PhoneNumber2;
                    gas.EmailAddress = model.EmailAddress;
                    gas.PreSalesStatus = model.GasPreSalesStatus;
                    gas.PreSalesFollowUpDate = DateTime.TryParse(model.GasPreSalesFollowUpDate, out DateTime gasPresalesDate) ? gasPresalesDate : (DateTime?)null;
                    gas.EMProcessor = model.EMProcessor;
                    gas.ContractChecked = model.ContractChecked;
                    gas.ContractAudited = model.ContractAudited;
                    gas.Terminated = model.Terminated;
                    gas.ContractNotes = model.ContractNotes;
                    gas.UpdatedAt = now;

                    // Update Gas contract dynamic fields (Electric-first priority for common fields)
                    gas.BrokerageId = model.BrokerageId;
                    gas.OfgemId = model.OfgemId;
                    gas.CloserId = model.CloserId;
                    gas.ReferralPartnerId = model.ReferralPartnerId;
                    gas.SubReferralPartnerId = model.SubReferralPartnerId;
                    gas.BrokerageStaffId = model.BrokerageStaffId;
                    gas.IntroducerId = model.IntroducerId;
                    gas.SubIntroducerId = model.SubIntroducerId;
                    gas.SubBrokerageId = model.SubBrokerageId;
                    gas.Collaboration = model.Collaboration;
                    gas.LeadGeneratorId = model.LeadGeneratorId;

                    if (triggeringStatuses.Contains(model.GasPreSalesStatus))
                        gas.InputDate = DateTime.Now.ToString("dd/MM/yyyy");

                    model.ElectricInputDate = electric.InputDate;
                    model.GasInputDate = gas.InputDate;

                    // Account
                    if (account != null)
                    {
                        account.SortCode = model.SortCode;
                        account.AccountNumber = model.AccountNumber;
                    }

                    // Logs
                    Log(_db.CE_ElectricContractLogs, model.ElectricPreSalesStatus, "Dual Electric contract updated.");
                    Log(_db.CE_GasContractLogs, model.GasPreSalesStatus, "Dual Gas contract updated.");

                    if (model.ContractChecked)
                    {
                        Log(_db.CE_ElectricContractLogs, model.ElectricPreSalesStatus, "Contract has been checked.");
                        Log(_db.CE_GasContractLogs, model.GasPreSalesStatus, "Contract has been checked.");
                    }

                    if (model.ContractAudited)
                    {
                        Log(_db.CE_ElectricContractLogs, model.ElectricPreSalesStatus, "Contract has been audited.");
                        Log(_db.CE_GasContractLogs, model.GasPreSalesStatus, "Contract has been audited.");
                    }

                    if (model.Terminated)
                    {
                        Log(_db.CE_ElectricContractLogs, model.ElectricPreSalesStatus, "Contract has been marked as terminated.");
                        Log(_db.CE_GasContractLogs, model.GasPreSalesStatus, "Contract has been marked as terminated.");
                    }

                    await _db.SaveChangesAsync();
                    tx.Commit();

                    return JsonResponse.Ok(new
                    {
                        EId = model.EId,
                        Department = electric.Department,
                        Source = electric.Source,
                        BusinessName = electric.BusinessName,
                        CustomerName = electric.CustomerName,
                        BusinessDoorNumber = electric.BusinessDoorNumber,
                        BusinessHouseName = electric.BusinessHouseName,
                        BusinessStreet = electric.BusinessStreet,
                        BusinessTown = electric.BusinessTown,
                        BusinessCounty = electric.BusinessCounty,
                        PostCode = electric.PostCode,
                        PhoneNumber1 = electric.PhoneNumber1,
                        PhoneNumber2 = electric.PhoneNumber2,
                        EmailAddress = electric.EmailAddress,
                        ElectricInputDate = electric.InputDate,
                        GasInputDate = gas.InputDate,
                        SortCode = account.SortCode,
                        AccountNumber = account.AccountNumber,
                        MPAN = electric.MPAN,
                        MPRN = gas.MPRN,
                        ElectricCurrentSupplier = electric.CurrentSupplier,
                        ElectricSalesType = electric.SalesType,
                        ElectricSalesTypeStatus = electric.SalesTypeStatus,
                        ElectricDuration = electric.Duration,
                        ElectricUplift = electric.Uplift,
                        ElectricInputEAC = electric.InputEAC,
                        ElectricStandingCharge = electric.StandingCharge,
                        ElectricDayRate = electric.DayRate,
                        ElectricNightRate = electric.NightRate,
                        ElectricEveWeekendRate = electric.EveWeekendRate,
                        ElectricOtherRate = electric.OtherRate,
                        ElectricSupplierId = electric.SupplierId,
                        ElectricProductId = electricSnapshotProduct.Id,
                        ElectricSupplierCommsType = electric.SupplierCommsType,
                        ElectricPreSalesStatus = electric.PreSalesStatus,
                        GasCurrentSupplier = gas.CurrentSupplier,
                        GasSalesType = gas.SalesType,
                        GasSalesTypeStatus = gas.SalesTypeStatus,
                        GasDuration = gas.Duration,
                        GasUplift = gas.Uplift,
                        GasStandingCharge = gas.StandingCharge,
                        GasUnitRate = gas.UnitRate,
                        GasSupplierId = gas.SupplierId,
                        GasProductId = gasSnapshotProduct.Id,
                        GasSupplierCommsType = gas.SupplierCommsType,
                        GasPreSalesStatus = gas.PreSalesStatus,
                        electric.ContractChecked,
                        electric.ContractAudited,
                        electric.Terminated,
                        TopLine = electric.TopLine,
                        ElectricInitialStartDate = electric.InitialStartDate,
                        GasInitialStartDate = gas.InitialStartDate,
                        GasInputEAC = gas.InputEAC,
                        GasOtherRate = gas.OtherRate,
                        ContractNotes = electric.ContractNotes,
                        EMProcessor = electric.EMProcessor

                    }, "Dual contract updated successfully!");
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    Logger.Log("EditDual failed: " + ex.Message);
                    return JsonResponse.Fail("An unexpected error occurred while updating dual contract.");
                }
            }
        }

        [HttpGet]
        [Authorize(Roles = "Pre-sales,Controls")]
        public async Task<JsonResult> GetLogsForDualContract(string eid)
        {
            try
            {
                var electricLogs = await _db.CE_ElectricContractLogs
                    .Where(l => l.EId == eid)
                    .Select(l => new LogEntry
                    {
                        EId = l.EId,
                        Username = l.Username,
                        ActionDate = l.ActionDate,
                        PreSalesStatusType = l.PreSalesStatusType,
                        Message = l.Message
                    })
                    .ToListAsync();

                var gasLogs = await _db.CE_GasContractLogs
                    .Where(l => l.EId == eid)
                    .Select(l => new LogEntry
                    {
                        EId = l.EId,
                        Username = l.Username,
                        ActionDate = l.ActionDate,
                        PreSalesStatusType = l.PreSalesStatusType,
                        Message = l.Message
                    })
                    .ToListAsync();

                var seen = new HashSet<string>();
                var finalLogs = new List<LogEntry>();

                foreach (var log in electricLogs.Concat(gasLogs))
                {
                    var key = $"{log.Username?.Trim()}|{log.ActionDate?.Trim()}|{log.PreSalesStatusType?.Trim()}|{log.Message?.Trim()}";
                    //Debug.WriteLine(key);
                    if (!seen.Contains(key))
                    {
                        seen.Add(key);
                        finalLogs.Add(new LogEntry
                        {
                            EId = log.EId,
                            Username = log.Username?.Trim(),
                            ActionDate = log.ActionDate?.Trim(),
                            PreSalesStatusType = log.PreSalesStatusType?.Trim(),
                            Message = log.Message?.Trim()
                        });
                    }
                }

                var sorted = finalLogs.OrderByDescending(l => ParserHelper.ParseDateForSorting(l.ActionDate))
                    .Select(l => new LogEntry
                    {
                        EId = l.EId,
                        Username = l.Username,
                        ActionDate = ParserHelper.FormatDateTimeForDisplay(l.ActionDate),
                        PreSalesStatusType = l.PreSalesStatusType,
                        Message = l.Message
                    }).ToList();
                return JsonResponse.Ok(sorted);
            }
            catch (Exception ex)
            {
                Logger.Log("GetLogsForDualContract failed: " + ex.Message);
                return JsonResponse.Fail("Unable to retrieve logs.");
            }
        }

        #endregion
    }
}