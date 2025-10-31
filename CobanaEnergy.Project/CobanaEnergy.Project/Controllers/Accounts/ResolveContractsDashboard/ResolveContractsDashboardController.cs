using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Filters;
using CobanaEnergy.Project.Helpers;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.Accounts.ResolveContractsDashboard;
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

namespace CobanaEnergy.Project.Controllers.Accounts.ResolveContractsDashboard
{
    [Authorize(Roles = "Accounts,Controls")]
    public class ResolveContractsDashboardController : BaseController
    {
        private readonly ApplicationDBContext db;
        private readonly List<string> resolveStatuses = new List<string>
        {
            "Resolve",
            "Never Live - Resolved"
        };

        public ResolveContractsDashboardController(ApplicationDBContext _db)
        {
            db = _db;
        }

        [HttpGet]
        public async Task<ActionResult> Index()
        {
            var suppliers = await db.CE_Supplier
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                })
                .ToListAsync();

            var model = new ResolveContractsDashboardViewModel
            {
                Suppliers = suppliers
            };

            return View("~/Views/Accounts/ResolveContractsDashboard/ResolveContractsDashboard.cshtml", model);
        }

        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> GetResolveContracts(int? supplierId, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var electricContractsQuery = db.CE_ElectricContracts.AsQueryable();
                var gasContractsQuery = db.CE_GasContracts.AsQueryable();

                if (supplierId.HasValue)
                {
                    electricContractsQuery = electricContractsQuery.Where(e => e.SupplierId == supplierId);
                    gasContractsQuery = gasContractsQuery.Where(g => g.SupplierId == supplierId);
                }

                var electricContractsList = await electricContractsQuery.ToListAsync();
                var gasContractsList = await gasContractsQuery.ToListAsync();

                if (startDate.HasValue && endDate.HasValue)
                {
                    var start = startDate.Value.Date;
                    var end = endDate.Value.Date;

                    electricContractsList = electricContractsList
                        .Where(e => DateTime.TryParse(e.InputDate, out var dt) && dt >= start && dt <= end)
                        .ToList();

                    gasContractsList = gasContractsList
                        .Where(g => DateTime.TryParse(g.InputDate, out var dt) && dt >= start && dt <= end)
                        .ToList();
                }
                else if (startDate.HasValue && !endDate.HasValue)
                {
                    return JsonResponse.Fail("Please select both dates!");
                }

                var electricContracts = electricContractsList
                    .Select(e => new
                    {
                        Contract = e,
                        Status = db.CE_ContractStatuses
                            .FirstOrDefault(cs => cs.EId == e.EId && cs.Type == "Electric" && resolveStatuses.Contains(cs.PaymentStatus)),
                        Reconciliation = db.CE_CommissionAndReconciliation
                            .FirstOrDefault(r => r.EId == e.EId && r.contractType == "Electric")
                    })
                    .Where(x => x.Status != null)
                    .ToList();

                var gasContracts = gasContractsList
                    .Select(g => new
                    {
                        Contract = g,
                        Status = db.CE_ContractStatuses
                            .FirstOrDefault(cs => cs.EId == g.EId && cs.Type == "Gas" && resolveStatuses.Contains(cs.PaymentStatus)),
                        Reconciliation = db.CE_CommissionAndReconciliation
                            .FirstOrDefault(r => r.EId == g.EId && r.contractType == "Gas")
                    })
                    .Where(x => x.Status != null)
                    .ToList();

                var contracts = electricContracts.Select(x => new ResolveContractsRowViewModel
                {
                    EId = x.Contract.EId,
                    BusinessName = x.Contract.BusinessName,
                    MPAN = x.Contract.MPAN,
                    MPRN = null,
                    InputEAC = x.Contract.InputEAC,
                    InputDate = ParserHelper.FormatDateForDisplay(x.Contract.InputDate),
                    StartDate = ParserHelper.FormatDateForDisplay(x.Reconciliation?.StartDate),
                    Duration = x.Contract.Duration,
                    PaymentStatus = x.Status.PaymentStatus ?? "N/A",
                    ContractStatus = x.Status.ContractStatus ?? "N/A",
                    CED = ParserHelper.FormatDateForDisplay(x.Reconciliation?.CED),
                    CEDCOT = ParserHelper.FormatDateForDisplay(x.Reconciliation?.CED_COT),
                    CobanaDueCommission = x.Reconciliation?.CobanaDueCommission ?? "N/A"
                }).ToList();

                contracts.AddRange(gasContracts.Select(x => new ResolveContractsRowViewModel
                {
                    EId = x.Contract.EId,
                    BusinessName = x.Contract.BusinessName,
                    MPAN = null,
                    MPRN = x.Contract.MPRN,
                    InputEAC = x.Contract.InputEAC,
                    InputDate = ParserHelper.FormatDateForDisplay(x.Contract.InputDate),
                    StartDate = ParserHelper.FormatDateForDisplay(x.Reconciliation?.StartDate),
                    Duration = x.Contract.Duration,
                    PaymentStatus = x.Status.PaymentStatus ?? "N/A",
                    ContractStatus = x.Status.ContractStatus ?? "N/A",
                    CED = ParserHelper.FormatDateForDisplay(x.Reconciliation?.CED),
                    CEDCOT = ParserHelper.FormatDateForDisplay(x.Reconciliation?.CED_COT),
                    CobanaDueCommission = x.Reconciliation?.CobanaDueCommission ?? "N/A"
                }));

                return JsonResponse.Ok(new
                {
                    Contracts = contracts
                });
            }
            catch (Exception ex)
            {
                Logger.Log("GetResolveContracts: " + ex);
                return JsonResponse.Fail("Error fetching resolve contracts.");
            }
        }

    }
}