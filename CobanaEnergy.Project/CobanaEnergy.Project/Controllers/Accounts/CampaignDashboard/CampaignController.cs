using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Filters;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.Accounts.AwaitingPaymentsDashboard;
using CobanaEnergy.Project.Models.Accounts.MainCampaign;
using CobanaEnergy.Project.Models.Accounts.MainCampaign.DBModel;
using Logic;
using Logic.ResponseModel.Helper;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Controllers.Accounts.CampaignDashboard
{
   
    public class CampaignController : BaseController
    {
        private readonly ApplicationDBContext db;
        private UserManager<ApplicationUser> _userManager => HttpContext.GetOwinContext().GetUserManager<UserManager<ApplicationUser>>();
        private RoleManager<IdentityRole> _roleManager => HttpContext.GetOwinContext().Get<RoleManager<IdentityRole>>();

        public CampaignController(ApplicationDBContext _db)
        {
            db = _db;
        }

        #region [Create Campaign]

        [HttpGet]
        [Authorize(Roles = "Accounts,Controls")]
        public async Task<ActionResult> Create()
        {
            try
            {
                ViewBag.Roles = await _roleManager.Roles.ToListAsync();
                return View("~/Views/Accounts/CampaignDashboard/Create.cshtml");
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"Failed to load roles: {ex.Message}";
                TempData["ToastType"] = "error";
                Logic.Logger.Log($"Campaign Page rendering failed!! {ex.Message}");
                return RedirectToAction("NotFound", "Error");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Accounts,Controls")]
        public async Task<JsonResult> Create(CampaignViewModel dto)
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

            try
            {
                var contract = new CE_Campaign
                {
                    CampaignName = dto.CampaignName,
                    SupplierId = dto.SupplierId,
                    ProductId = dto.ProductId,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    SaleTarget = dto.SaleTarget,
                    Bonus = dto.Bonus
                };

                db.CE_Campaigns.Add(contract);
                await db.SaveChangesAsync();

                return JsonResponse.Ok(new { redirectUrl = Url.Action("Index", "Campaign") }, "Campaign created successfully!");
            }
            catch (Exception ex)
            {
                Logger.Log("Create Campaign failed: " + ex.Message);
                return JsonResponse.Fail("An unexpected error occurred while saving the campaign.");
            }
        }

        #endregion

        #region [campaign dashboard]

        [HttpGet]
        public async Task<ActionResult> Index()
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
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                })
                .ToList();

            var model = new CampaignDashboardViewModel
            {
                Suppliers = filtered
            };

            return View("~/Views/Accounts/CampaignDashboard/CampaignDashboard.cshtml", model);
        }

        [HttpGet]
        public async Task<JsonResult> GetCampaignBySupplier(long supplierId)
        {
            try
            {
                var today = DateTime.Today;
                var rawCampaigns = await db.CE_Campaigns
                    .Where(p => p.SupplierId == supplierId)
                    .ToListAsync();

                var campaigns = rawCampaigns
                    .Where(p =>
                        p.EndDate > today)
                    .Select(p => new { p.CId, CampaignName = p.CampaignName })
                    .ToList();

                return JsonResponse.Ok(campaigns);
            }
            catch (Exception ex)
            {
                Logger.Log("GetCampaignBySupplier: " + ex.Message);
                return JsonResponse.Fail("Unable to load campaigns.");
            }
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> GetCampaignList(int? supplierId, Guid? cId)
        {
            try
            {
                var validStatuses = new[] { "Submitted", "Overturned Contract" };

                var campaigns = await db.CE_Campaigns
                    .Where(c =>
                        (!supplierId.HasValue || c.SupplierId == supplierId) &&
                        (!cId.HasValue || c.CId == cId))
                    .ToListAsync();

                if (!campaigns.Any())
                    return JsonResponse.Fail("No campaigns found.");

                var allContracts = new List<CampaignDashboardRowViewModel>();
                int totalBonus = 0;
                int saleTarget = 0;

                foreach (var campaign in campaigns)
                {
                    int bonusPerContract = int.TryParse(campaign.Bonus, out var bVal) ? bVal : 0;
                    saleTarget = int.TryParse(campaign.SaleTarget, out var sVal) ? sVal : 0;

                    var gasData = await db.CE_GasContracts
                        .Where(gc =>
                            gc.SupplierId == campaign.SupplierId &&
                            validStatuses.Contains(gc.PreSalesStatus))
                        .ToListAsync();

                    var electricData = await db.CE_ElectricContracts
                        .Where(ec =>
                            ec.SupplierId == campaign.SupplierId &&
                            validStatuses.Contains(ec.PreSalesStatus))
                        .ToListAsync();

                    var filteredGas = gasData
                        .Where(gc =>
                            DateTime.TryParse(gc.InputDate, out var inputDate) &&
                            inputDate.Date >= campaign.StartDate.Date &&
                            inputDate.Date <= (campaign.EndDate.Date) &&
                            DateTime.TryParse(gc.CreatedAt, out var createdAt) &&
                            createdAt.Date >= campaign.CreatedAt.Date)
                        .Select(gc => new CampaignDashboardRowViewModel
                        {
                            Id = gc.Id,
                            BusinessName = gc.BusinessName,
                            Number = gc.MPRN,
                            CreatedAt = DateTime.TryParse(gc.CreatedAt, out var dt)
                                        ? dt.ToString("dd-MM-yy")
                                        : "N/A",
                            Bonus = campaign.Bonus ?? "0"
                        }).ToList();

                    var filteredElectric = electricData
                        .Where(ec =>
                            DateTime.TryParse(ec.InputDate, out var inputDate) &&
                            inputDate.Date >= campaign.StartDate.Date &&
                            inputDate.Date <= (campaign.EndDate.Date) &&
                            DateTime.TryParse(ec.CreatedAt, out var createdAt) &&
                            createdAt.Date >= campaign.CreatedAt.Date)
                        .Select(ec => new CampaignDashboardRowViewModel
                        {
                            Id = ec.Id,
                            BusinessName = ec.BusinessName,
                            Number = ec.MPAN,
                            CreatedAt = DateTime.TryParse(ec.CreatedAt, out var dt)
                                        ? dt.ToString("dd-MM-yy")
                                        : "N/A",
                            Bonus = campaign.Bonus ?? "0"
                        }).ToList();

                    allContracts.AddRange(filteredGas);
                    allContracts.AddRange(filteredElectric);

                    totalBonus += (filteredGas.Count + filteredElectric.Count) * bonusPerContract;
                }

                var result = new CampaignDashboardViewModel
                {
                    SupplierId = supplierId,
                    CampaignId = cId ?? Guid.Empty,
                    Campaigns = allContracts,
                    Count = allContracts.Count,
                    TotalBonus = totalBonus,
                    TargetAchieved = allContracts.Count >= saleTarget
                };

                return JsonResponse.Ok(result);
            }
            catch (Exception ex)
            {
                Logger.Log("GetCampaignList: " + ex);
                return JsonResponse.Fail("Error fetching campaign data.");
            }
        }



        #endregion
    }
}