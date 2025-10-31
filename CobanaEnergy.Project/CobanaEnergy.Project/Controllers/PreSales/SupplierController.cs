using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Filters;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.Supplier;
using CobanaEnergy.Project.Models.Supplier.Edit_Supplier;
using CobanaEnergy.Project.Models.Supplier.Supplier_Dashboard;
using CobanaEnergy.Project.Models.Supplier.SupplierDBModels;
using CobanaEnergy.Project.Models.Sector.SectorDBModels;
using Logic;
using Logic.ResponseModel.Helper;
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
    public class SupplierController : BaseController
    {
        private readonly ApplicationDBContext db;

        public SupplierController(ApplicationDBContext _db)
        {
            db = _db;
        }

        #region supplier_creation

        [HttpGet]
        [Authorize(Roles = "Controls")]
        public ActionResult SupplierCreation()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Controls")]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> SupplierCreation(SupplierViewModel model)
        {
            if (!ModelState.IsValid)
            {
                //return JsonResponse.Fail("Please correct the errors in the form.");
                var errors = ModelState.Values.SelectMany(x => x.Errors).Select(e => e.ErrorMessage).ToList();
                return JsonResponse.Fail(string.Join("<br>", errors));
            }

            if (model.Uplifts.Count <= 0)
            {
                return JsonResponse.Fail("Please add at least one uplift for the supplier.");
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var supplier = new CE_Supplier
                    {
                        Name = model.Name,
                        Status = true,
                        Link = model.Link,
                        CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    };

                    db.CE_Supplier.Add(supplier);
                    await db.SaveChangesAsync();

                    foreach (var contact in model.Contacts)
                    {
                        db.CE_SupplierContacts.Add(new CE_SupplierContacts
                        {
                            SupplierId = supplier.Id,
                            ContactName = contact.ContactName,
                            Role = contact.Role,
                            PhoneNumber = contact.PhoneNumber,
                            Email = contact.Email,
                            Notes = contact.Notes
                        });
                    }

                    foreach (var product in model.Products)
                    {
                        db.CE_SupplierProducts.Add(new CE_SupplierProducts
                        {
                            SupplierId = supplier.Id,
                            ProductName = product.ProductName,
                            StartDate = product.StartDate,
                            EndDate = "3099-06-23",// product.EndDate,
                            Commission = product.Commission,
                            SupplierCommsType = product.SupplierCommsType
                        });
                    }

                    foreach (var uplift in model.Uplifts)
                    {
                        bool upliftActiveConflict = await db.CE_SupplierUplifts.AnyAsync(u =>
                            u.SupplierId == supplier.Id &&
                            u.FuelType == uplift.FuelType &&
                            u.EndDate > DateTime.Now);

                        if (upliftActiveConflict)
                        {
                            transaction.Rollback();
                            return JsonResponse.Fail($"Cannot add new {uplift.FuelType} uplift while a previous one is still active.");
                        }

                        db.CE_SupplierUplifts.Add(new CE_SupplierUplifts
                        {
                            SupplierId = supplier.Id,
                            FuelType = uplift.FuelType,
                            Uplift = uplift.Uplift,
                            StartDate = uplift.StartDate,
                            EndDate = DateTime.Parse("3099-06-23 23:55:00.000") // uplift.EndDate
                        });
                    }

                    await db.SaveChangesAsync();
                    transaction.Commit();

                    //return JsonResponse.Ok("Supplier created successfully!");
                    return JsonResponse.Ok(new { redirectUrl = Url.Action("SupplierDashboard", "Supplier") }, "Supplier created successfully!");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Logger.Log($"Supplier creation failed: {ex.Message}");
                    return JsonResponse.Fail("An unexpected error occurred while saving supplier.");
                }
            }
        }

        #endregion

        #region supplier_dashboard

        [HttpGet]
        [Authorize(Roles = "Controls")]
        public ActionResult SupplierDashboard()
        {
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "Controls")]
        public async Task<JsonResult> GetActiveSuppliers()
        {
            try
            {
                var today = DateTime.Today;

                var suppliers = await db.CE_Supplier
                    .Where(s => s.Status)
                    .Include(s => s.CE_SupplierProducts).Where(s => s != null)
                    .ToListAsync();

                var data = suppliers
                    .SelectMany(s => s.CE_SupplierProducts
                        .Where(p =>
                            DateTime.TryParse(p.EndDate, out var endDate) &&
                            endDate > today)
                        .Select(p => new SupplierDashboardItemViewModel
                        {
                            SupplierId = s.Id,
                            SupplierName = s.Name,
                            Link = s.Link,
                            ProductName = p.ProductName,
                            Commission = p.Commission,
                            Status = s.Status
                        }))
                    .ToList();

                return JsonResponse.Ok(data);
            }
            catch (Exception ex)
            {
                Logger.Log("Failed to load suppliers dashboard: " + ex.Message);
                return JsonResponse.Fail("Failed to load suppliers.");
            }
        }

        #endregion

        #region edit_supplier

        [HttpGet]
        [Authorize(Roles = "Controls")]
        public ActionResult EditSupplier()
        {
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "Controls")]
        public async Task<JsonResult> GetSupplierForEdit(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id) || !int.TryParse(id, out int supplierId))
                    return JsonResponse.FailRedirection(new { redirectUrl = Url.Action("SupplierDashboard", "Supplier") }, "Invalid or missing supplier ID.");

                var supplier = await db.CE_Supplier
                    .Include(s => s.CE_SupplierContacts)
                    .Include(s => s.CE_SupplierProducts)
                    .FirstOrDefaultAsync(s => s.Id == supplierId && s.Status);

                if (supplier == null)
                    return JsonResponse.FailRedirection(new { redirectUrl = Url.Action("SupplierDashboard", "Supplier") }, "Supplier not found or inactive.");

                var uplifts = await db.CE_SupplierUplifts
                            .Where(u => u.SupplierId == supplier.Id)
                            .ToListAsync();

                var viewModel = new EditSupplierViewModel
                {
                    Id = supplier.Id,
                    Name = supplier.Name,
                    Link = supplier.Link,
                    Status = supplier.Status,
                    Contacts = supplier.CE_SupplierContacts.Select(c => new SupplierContactViewModel
                    {
                        Id = c.Id,
                        ContactName = c.ContactName,
                        Role = c.Role,
                        PhoneNumber = c.PhoneNumber,
                        Email = c.Email,
                        Notes = c.Notes
                    }).ToList(),
                    Uplifts = uplifts.Select(u => new SupplierUpliftViewModel
                    {
                        Id = u.Id,
                        FuelType = u.FuelType,
                        Uplift = u.Uplift,
                        StartDate = u.StartDate.ToString("yyyy-MM-ddTHH:mm"),
                        EndDate = u.EndDate.ToString("yyyy-MM-ddTHH:mm")
                    }).ToList(),
                    Products = supplier.CE_SupplierProducts.Select(p => new SupplierProductViewModel
                    {
                        Id = p.Id,
                        ProductName = p.ProductName,
                        StartDate = p.StartDate,
                        EndDate = p.EndDate,
                        Commission = p.Commission,
                        SupplierCommsType = p.SupplierCommsType

                    }).ToList()
                };

                return JsonResponse.Ok(viewModel);
            }
            catch (Exception ex)
            {
                Logger.Log("Error loading supplier for edit: " + ex.Message);
                return JsonResponse.Fail("Something went wrong while loading the supplier.");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Controls")]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> EditSupplier(EditSupplierViewModel model)
        {
            if (!ModelState.IsValid)
            {
                //return JsonResponse.Fail("Please correct the errors in the form.");
                var errors = ModelState.Values.SelectMany(x => x.Errors).Select(e => e.ErrorMessage).ToList();
                return JsonResponse.Fail(string.Join("<br>", errors));
            }

            if (model.Uplifts.Count <= 0)
            {
                return JsonResponse.Fail("Please add at least one uplift for the supplier.");
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var supplier = await db.CE_Supplier
                        .Include(s => s.CE_SupplierContacts)
                        .Include(s => s.CE_SupplierProducts)
                        .FirstOrDefaultAsync(s => s.Id == model.Id);

                    if (supplier == null)
                        return JsonResponse.FailRedirection(new { redirectUrl = Url.Action("SupplierDashboard", "Supplier") }, "Supplier not found.");

                    supplier.Name = model.Name;
                    supplier.Status = model.Status;
                    supplier.Link = model.Link;

                    await db.SaveChangesAsync();

                    var submittedContactIds = model.Contacts.Where(c => c.Id > 0).Select(c => c.Id).ToList();
                    var contactsToDelete = supplier.CE_SupplierContacts
                        .Where(c => !submittedContactIds.Contains(c.Id))
                        .ToList();

                    db.CE_SupplierContacts.RemoveRange(contactsToDelete);
                    await db.SaveChangesAsync();

                    foreach (var contact in model.Contacts)
                    {
                        if (contact.Id > 0)
                        {
                            var existing = supplier.CE_SupplierContacts.FirstOrDefault(c => c.Id == contact.Id);
                            if (existing != null)
                            {
                                existing.ContactName = contact.ContactName;
                                existing.Role = contact.Role;
                                existing.PhoneNumber = contact.PhoneNumber;
                                existing.Email = contact.Email;
                                existing.Notes = contact.Notes;
                            }
                        }
                        else
                        {
                            db.CE_SupplierContacts.Add(new CE_SupplierContacts
                            {
                                SupplierId = supplier.Id,
                                ContactName = contact.ContactName,
                                Role = contact.Role,
                                PhoneNumber = contact.PhoneNumber,
                                Email = contact.Email,
                                Notes = contact.Notes
                            });
                        }
                    }

                    await db.SaveChangesAsync();

                    var submittedProductIds = model.Products.Where(p => p.Id > 0).Select(p => p.Id).ToList();

                    var productIdsInUseElectric = db.CE_ElectricContracts
                    .Select(e => e.ProductId)
                    .Distinct();

                    var productIdsInUseGas = db.CE_GasContracts
                        .Select(g => g.ProductId)
                        .Distinct();

                    var productIdsInUse = productIdsInUseElectric
                        .Union(productIdsInUseGas)
                        .Distinct()
                        .ToList();

                    var productsToDelete = supplier.CE_SupplierProducts
                        .Where(p => !submittedProductIds.Contains(p.Id))
                        .ToList();

                    var inUseProducts = productsToDelete
                        .Where(p => productIdsInUse.Contains(p.Id))
                        .ToList();

                    var deletableProducts = productsToDelete
                        .Where(p => !productIdsInUse.Contains(p.Id))
                        .ToList();

                    db.CE_SupplierProducts.RemoveRange(deletableProducts);
                    await db.SaveChangesAsync();

                    foreach (var product in model.Products)
                    {
                        if (product.Id > 0)
                        {
                            var existing = supplier.CE_SupplierProducts.FirstOrDefault(p => p.Id == product.Id);
                            if (existing != null)
                            {
                                existing.ProductName = product.ProductName;
                                existing.StartDate = product.StartDate;
                                existing.EndDate = product.EndDate;
                                existing.Commission = product.Commission;
                                existing.SupplierCommsType = product.SupplierCommsType;
                            }
                        }
                        else
                        {
                            db.CE_SupplierProducts.Add(new CE_SupplierProducts
                            {
                                SupplierId = supplier.Id,
                                ProductName = product.ProductName,
                                StartDate = product.StartDate,
                                EndDate = product.EndDate,
                                Commission = product.Commission,
                                SupplierCommsType = product.SupplierCommsType
                            });
                        }
                    }

                    await db.SaveChangesAsync();

                    foreach (var uplift in model.Uplifts)
                    {
                        if (!DateTime.TryParseExact(uplift.StartDate, "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
                            || !DateTime.TryParseExact(uplift.EndDate, "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                        {
                            return JsonResponse.Fail("Invalid date format found in one of the uplift entries. Please fix and try again.");
                        }
                    }

                    var now = DateTime.Now;

                    foreach (var fuelType in model.Uplifts.Select(u => u.FuelType).Distinct())
                    {
                        var activeInModel = model.Uplifts.Count(u =>
                            u.FuelType == fuelType &&
                            DateTime.TryParseExact(u.EndDate, "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var end) &&
                            end > now);

                        if (activeInModel > 1)
                        {
                            return JsonResponse.Fail($"Only one active uplift is allowed for {fuelType}. Please adjust existing entries.");
                        }

                        var modelUpliftIds = model.Uplifts.Select(u => u.Id).ToList();

                        var dbDuplicate = await db.CE_SupplierUplifts.AnyAsync(u =>
                            u.SupplierId == supplier.Id &&
                            u.FuelType == fuelType &&
                            !modelUpliftIds.Contains(u.Id) &&
                            (u.EndDate == null || u.EndDate > now));

                        if (activeInModel == 1 && dbDuplicate)
                        {
                            return JsonResponse.Fail($"Only one active uplift is allowed for {fuelType}. An active uplift already exists in the database.");
                        }
                    }

                    var submittedUpliftIds = model.Uplifts.Where(u => u.Id > 0).Select(u => u.Id).ToList();
                    var upliftsToDelete = db.CE_SupplierUplifts
                        .Where(u => u.SupplierId == supplier.Id && !submittedUpliftIds.Contains(u.Id))
                        .ToList();
                    db.CE_SupplierUplifts.RemoveRange(upliftsToDelete);

                    foreach (var uplift in model.Uplifts)
                    {
                        DateTime.TryParseExact(uplift.StartDate, "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDate);
                        DateTime.TryParseExact(uplift.EndDate, "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endDate);

                        if (uplift.Id > 0)
                        {
                            var existing = await db.CE_SupplierUplifts.FirstOrDefaultAsync(u => u.Id == uplift.Id);
                            if (existing != null)
                            {
                                existing.FuelType = uplift.FuelType;
                                existing.Uplift = uplift.Uplift;
                                existing.StartDate = startDate;
                                existing.EndDate = endDate;
                            }
                        }
                        else
                        {
                            db.CE_SupplierUplifts.Add(new CE_SupplierUplifts
                            {
                                SupplierId = supplier.Id,
                                FuelType = uplift.FuelType,
                                Uplift = uplift.Uplift,
                                StartDate = startDate,
                                EndDate = endDate
                            });
                        }
                    }

                    await db.SaveChangesAsync();
                    transaction.Commit();

                    if (inUseProducts.Any())
                    {
                        var names = string.Join(", ", inUseProducts.Select(p => p.ProductName));
                        return JsonResponse.Ok(new
                        {
                            success = true,
                            // partialWarning = true
                        }, $"Supplier updated successfully. However, the following product(s) could not be deleted as they are currently in use: {names} Please referesh the page");
                    }

                    return JsonResponse.Ok(new { redirectUrl = Url.Action("SupplierDashboard", "Supplier") }, "Supplier updated successfully!");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Logger.Log("Edit supplier failed: " + ex.Message);
                    return JsonResponse.Fail("An unexpected error occurred while updating the supplier.");
                }
            }
        }

        #endregion

        #region getting_active_suppliers

        [HttpGet]
        [Authorize]
        public async Task<JsonResult> GetActiveSuppliersForDropdown()
        {
            try
            {
                var today = DateTime.Today;

                var suppliers = await db.CE_Supplier
                    .Where(s => s.Status)
                    .Include(s => s.CE_SupplierProducts)
                    .ToListAsync();

                var filtered = suppliers
                    .Where(s => s.CE_SupplierProducts.Any(p =>
                        !string.IsNullOrWhiteSpace(p.EndDate) &&
                        DateTime.TryParse(p.EndDate, out var endDate) &&
                        endDate > today))
                    .Select(s => new
                    {
                        s.Id,
                        Name = s.Name
                    })
                    .ToList();

                return JsonResponse.Ok(filtered);
            }
            catch (Exception ex)
            {
                Logger.Log("GetActiveSuppliersForDropdown failed: " + ex.Message);
                return JsonResponse.Fail("Unable to load active suppliers.");
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<JsonResult> GetActiveSuppliersBySector(int sectorId)
        {
            try
            {
                var today = DateTime.Today;

                // Get suppliers linked to the specific sector
                var sectorSuppliers = await db.CE_SectorSupplier
                    .Where(ss => ss.SectorId == sectorId)
                    .Include(ss => ss.Supplier)
                    .ToListAsync();

                // Get supplier IDs from sector suppliers
                var supplierIds = sectorSuppliers.Select(ss => ss.SupplierId).ToList();

                // Get suppliers with their products (similar to GetActiveSuppliersForDropdown)
                var suppliers = await db.CE_Supplier
                    .Where(s => s.Status && supplierIds.Contains(s.Id))
                    .Include(s => s.CE_SupplierProducts)
                    .ToListAsync();

                // Filter suppliers that have active products
                var filtered = suppliers
                    .Where(s => s.CE_SupplierProducts.Any(p =>
                        !string.IsNullOrWhiteSpace(p.EndDate) &&
                        DateTime.TryParse(p.EndDate, out var endDate) &&
                        endDate > today))
                    .Select(s => new
                    {
                        s.Id,
                        Name = s.Name
                    })
                    .ToList();

                return JsonResponse.Ok(filtered);
            }
            catch (Exception ex)
            {
                Logger.Log("GetActiveSuppliersBySector failed: " + ex.Message);
                return JsonResponse.Fail("Unable to load suppliers for the selected sector.");
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<JsonResult> GetProductsBySupplier(long supplierId)
        {
            try
            {
                var today = DateTime.Today;

                var rawProducts = await db.CE_SupplierProducts
                    .Where(p => p.SupplierId == supplierId)
                    .ToListAsync();

                var products = rawProducts
                    .Where(p =>
                        !string.IsNullOrWhiteSpace(p.EndDate) &&
                        DateTime.Parse(p.EndDate) > today)
                    .Select(p => new { p.Id, ProductName = p.ProductName, SupplierCommsType = p.SupplierCommsType })
                    .ToList();

                return JsonResponse.Ok(products);
            }
            catch (Exception ex)
            {
                Logger.Log("GetProductsBySupplier: " + ex.Message);
                return JsonResponse.Fail("Unable to load products.");
            }
        }



        #endregion

        #region supplier_uplift

        [HttpGet]
        [Authorize]
        public async Task<JsonResult> GetActiveUpliftForSupplier(int supplierId, string fuelType)
        {
            try
            {
                var now = DateTime.Now;

                var uplift = await db.CE_SupplierUplifts
                    .Where(u => u.SupplierId == supplierId && u.FuelType == fuelType && (u.EndDate == null || u.EndDate > now))
                    .OrderByDescending(u => u.StartDate)
                    .Select(u => u.Uplift)
                    .FirstOrDefaultAsync();

                return Json(new { success = true, Data = uplift }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Logger.Log("Failed to load active suppliers Uplifts : " + ex.Message);
                return JsonResponse.Fail("Failed to check suppliers uplift. Please check with IT");
            }
        }

        [HttpGet]
        [Authorize]
        public JsonResult GetSnapshotMaxUpliftElectric(string eid)
        {
            var snapshot = db.CE_ElectricSupplierSnapshots
                             .Include(s => s.CE_ElectricSupplierUpliftSnapshots)
                             .FirstOrDefault(s => s.EId == eid);

            if (snapshot == null)
                return JsonResponse.Fail("Snapshot not found.");

            var maxUplift = snapshot.CE_ElectricSupplierUpliftSnapshots
                                    .Where(u => u.FuelType == "Electric")
                                    .OrderByDescending(u => u.EndDate)
                                    .FirstOrDefault();

            if (maxUplift == null)
                return JsonResponse.Ok(null);

            return JsonResponse.Ok(maxUplift.Uplift);
        }

        [HttpGet]
        [Authorize]
        public JsonResult GetSnapshotMaxUpliftGas(string eid)
        {
            var snapshot = db.CE_GasSupplierSnapshots
                             .Include(s => s.CE_GasSupplierUpliftSnapshots)
                             .FirstOrDefault(s => s.EId == eid);

            if (snapshot == null)
                return JsonResponse.Fail("Snapshot not found.");

            var maxUplift = snapshot.CE_GasSupplierUpliftSnapshots
                                    .Where(u => u.FuelType == "Gas")
                                    .OrderByDescending(u => u.EndDate)
                                    .FirstOrDefault();

            if (maxUplift == null)
                return JsonResponse.Ok(null);

            return JsonResponse.Ok(maxUplift.Uplift);
        }

        #endregion
    }
}