using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Helpers;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.Supplier.Active_Suppliers;
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
    public class PreSalesController : BaseController
    {
        private readonly ApplicationDBContext db;
        public PreSalesController(ApplicationDBContext _db)
        {
            db = _db;
        }

        #region contract_listing

        [Authorize(Roles = "Pre-sales,Controls")]
        [HttpGet]
        public ActionResult Dashboard()
        {
            return View("Dashboard");
        }

        [Authorize(Roles = "Pre-sales,Controls")]
        [HttpGet]
        public async Task<JsonResult> GetAllContracts()
        {
            try
            {
                var excludeStatuses = new List<string>
        {
            "Meter Registration Submitted", "New Connection Submitted", "Overturned Contract", "Submitted",
            "Rejected Contract", "Failed Audit Call", "Failed Credit Check", "New Connection Failed Credit",
            "Meter Registration Failed Credit", "Incorrect Prices", "Incorrect Rejected Recording",
            "Meter not Supported (De-Energised)", "Duplicate Contract"
        };

                var electricContracts = await db.CE_ElectricContracts
                    .Select(e => new
                    {
                        e.EId,
                        e.BusinessName,
                        e.CustomerName,
                        InputDate = e.InputDate,
                        PreSalesStatus = e.PreSalesStatus,
                        e.ContractNotes,
                        MPAN = e.MPAN,
                        MPRN = "",
                        Type = e.Type,
                        // Fields needed for Agent logic
                        Department = e.Department,
                        CloserId = e.CloserId,
                        BrokerageStaffId = e.BrokerageStaffId,
                        SubIntroducerId = e.SubIntroducerId
                    }).ToListAsync();

                var gasContracts = await db.CE_GasContracts
                    .Select(g => new
                    {
                        g.EId,
                        g.BusinessName,
                        g.CustomerName,
                        InputDate = g.InputDate,
                        PreSalesStatus = g.PreSalesStatus,
                        g.ContractNotes,
                        MPAN = "",
                        MPRN = g.MPRN,
                        Type = g.Type,
                        // Fields needed for Agent logic
                        Department = g.Department,
                        CloserId = g.CloserId,
                        BrokerageStaffId = g.BrokerageStaffId,
                        SubIntroducerId = g.SubIntroducerId
                    }).ToListAsync();

                var combined = electricContracts
                    .Concat(gasContracts)
                    .GroupBy(x => x.EId)
                    .Select(g =>
                    {
                        var all = g.ToList();

                        var elec = all.FirstOrDefault(x => (x.Type == "Electric" || x.Type == "Dual") && !string.IsNullOrEmpty(x.MPAN));
                        var gas = all.FirstOrDefault(x => (x.Type == "Gas" || x.Type == "Dual") && !string.IsNullOrEmpty(x.MPRN));

                        bool isDual = elec != null && gas != null;

                        bool elecExcluded = elec != null && excludeStatuses.Contains((elec.PreSalesStatus ?? "").Trim());
                        bool gasExcluded = gas != null && excludeStatuses.Contains((gas.PreSalesStatus ?? "").Trim());

                        if (!isDual && elec != null && elecExcluded)
                            return null;

                        if (!isDual && gas != null && gasExcluded)
                            return null;

                        if (isDual && elecExcluded && gasExcluded)
                            return null;

                        string mergedStatus;
                        var elecsafeStatus = System.Net.WebUtility.HtmlEncode(elec?.PreSalesStatus);
                        var gassafeStatus = System.Net.WebUtility.HtmlEncode(gas?.PreSalesStatus);
                        if (isDual)
                        {
                            if (elecExcluded && !gasExcluded)
                            {
                                mergedStatus = $"<span class='highlight-red'>Electric: {elecsafeStatus}</span><br>Gas: {gassafeStatus}";
                            }
                            else if (gasExcluded && !elecExcluded)
                            {
                                mergedStatus = $"Electric: {elecsafeStatus}<br><span class='highlight-red'>Gas: {gassafeStatus}</span>";
                            }
                            else
                            {
                                mergedStatus = $"Electric: {elecsafeStatus}<br>Gas: {gassafeStatus}";
                            }
                        }
                        else
                        {
                            if (elec != null)
                                mergedStatus = elecExcluded ? $"<span class='highlight-red'>{elecsafeStatus}</span>" : elecsafeStatus;
                            else if (gas != null)
                                mergedStatus = gasExcluded ? $"<span class='highlight-red'>{gassafeStatus}</span>" : gassafeStatus;
                            else
                                mergedStatus = "-";
                        }

                        string mergedInputDate = isDual
                            ? string.Join("<br>", new[]
                            {
                        elec != null ? $"Electric: {ParserHelper.FormatDateForDisplay(elec.InputDate)}" : null,
                        gas != null ? $"Gas: {ParserHelper.FormatDateForDisplay(gas.InputDate)}" : null
                            }.Where(x => !string.IsNullOrWhiteSpace(x)))
                            : ParserHelper.FormatDateForDisplay(elec?.InputDate ?? gas?.InputDate ?? "-");

                        DateTime elecDate = ParserHelper.ParseDateForSorting(elec?.InputDate);
                        DateTime gasDate = ParserHelper.ParseDateForSorting(gas?.InputDate);
                        
                        // Use the most recent date for consistent sorting across all contract types
                        DateTime sortable = elecDate > gasDate ? elecDate : gasDate;
                        
                        // Ensure we have a valid sortable date - use a very old date for invalid dates
                        if (sortable == DateTime.MinValue)
                        {
                            sortable = new DateTime(1900, 1, 1); // Use a fixed old date as fallback
                        }

                        // Determine Agent based on department type (use electric data if available, otherwise gas)
                        var contractData = elec ?? gas;
                        string agentName = "-";
                        
                        if (contractData != null)
                        {
                            if (contractData.Department == "In House" && contractData.CloserId.HasValue)
                            {
                                var closer = db.CE_Sector
                                    .Where(s => s.SectorID == contractData.CloserId && s.SectorType == "closer")
                                    .Select(s => s.Name)
                                    .FirstOrDefault();
                                agentName = closer ?? "-";
                            }
                            else if (contractData.Department == "Brokers" && contractData.BrokerageStaffId.HasValue)
                            {
                                var brokerageStaff = db.CE_BrokerageStaff
                                    .Where(bs => bs.BrokerageStaffID == contractData.BrokerageStaffId)
                                    .Select(bs => bs.BrokerageStaffName)
                                    .FirstOrDefault();
                                agentName = brokerageStaff ?? "-";
                            }
                            else if (contractData.Department == "Introducers" && contractData.SubIntroducerId.HasValue)
                            {
                                var subIntroducer = db.CE_SubIntroducer
                                    .Where(si => si.SubIntroducerID == contractData.SubIntroducerId)
                                    .Select(si => si.SubIntroducerName)
                                    .FirstOrDefault();
                                agentName = subIntroducer ?? "-";
                            }
                        }

                        return new
                        {
                            EId = g.Key,
                            Agent = agentName,
                            MPAN = !string.IsNullOrWhiteSpace(elec?.MPAN) && elec.MPAN != "N/A" ? elec.MPAN : "N/A",
                            MPRN = !string.IsNullOrWhiteSpace(gas?.MPRN) && gas.MPRN != "N/A" ? gas.MPRN : "N/A",
                            BusinessName = elec?.BusinessName ?? gas?.BusinessName ?? "-",
                            CustomerName = elec?.CustomerName ?? gas?.CustomerName ?? "-",
                            InputDate = mergedInputDate,
                            PreSalesStatus = mergedStatus,
                            Notes = elec?.ContractNotes ?? gas?.ContractNotes ?? "-----",
                            Type = isDual ? "Dual" : elec != null ? "Electric" : "Gas",
                            SortableDate = sortable.ToString("yyyy-MM-dd")
                        };
                    })
                    .Where(x => x != null)
                    .ToList();

                return JsonResponse.Ok(combined);
            }
            catch (Exception ex)
            {
                Logger.Log("GetAllContracts: " + ex);
                return JsonResponse.Fail("Failed to fetch contract records.");
            }
        }

        #endregion

        #region active_suppliers

        [HttpGet]
        [Authorize(Roles = "Pre-sales,Controls")]
        public async Task<JsonResult> GetSupplierContractStats()
        {
            try
            {
                var todayStart = DateTime.Today.AddHours(8);
                var todayEnd = DateTime.Today.AddHours(19);
                var todayPrefix = DateTime.Today.ToString("dd/MM/yyyy ");

                var suppliers = await db.CE_Supplier.ToListAsync();

                var electricRaw = await db.CE_ElectricContracts
                    .Where(e => e.CreatedAt != null && e.CreatedAt.StartsWith(todayPrefix))
                    .ToListAsync();

                var gasRaw = await db.CE_GasContracts
                    .Where(g => g.CreatedAt != null && g.CreatedAt.StartsWith(todayPrefix))
                    .ToListAsync();


                var electricFiltered = electricRaw
                    .Where(e =>
                        DateTime.TryParse(e.CreatedAt, out var dt) &&
                        dt >= todayStart && dt <= todayEnd &&
                        e.SupplierId != 0)
                    .Select(e => new
                    {
                        SupplierId = e.SupplierId,
                        PreSalesStatus = e.PreSalesStatus,
                        Type = "Electric"
                    });

                var gasFiltered = gasRaw
                    .Where(g =>
                        DateTime.TryParse(g.CreatedAt, out var dt) &&
                        dt >= todayStart && dt <= todayEnd &&
                        g.SupplierId != 0)
                    .Select(g => new
                    {
                        SupplierId = g.SupplierId,
                        PreSalesStatus = g.PreSalesStatus,
                        Type = "Gas"
                    });

                var combined = electricFiltered
                    .Concat(gasFiltered)
                    .GroupBy(x => new { x.SupplierId, x.PreSalesStatus })
                    .Select(group =>
                    {
                        var supplier = suppliers.FirstOrDefault(s => s.Id == group.Key.SupplierId);
                        return new SupplierContractStatViewModel
                        {
                            SupplierName = supplier?.Name ?? "Unknown",
                            PreSalesStatus = group.Key.PreSalesStatus ?? "-",
                            ElectricCount = group.Count(x => x.Type == "Electric"),
                            GasCount = group.Count(x => x.Type == "Gas")
                        };
                    })
                    .OrderBy(x => x.SupplierName)
                    .ThenBy(x => x.PreSalesStatus)
                    .ToList();

                return JsonResponse.Ok(combined);
            }
            catch (Exception ex)
            {
                Logger.Log("GetSupplierContractStats: " + ex);
                return JsonResponse.Fail("Failed to fetch supplier stats.");
            }
        }

        #endregion

        #region LockManagement

        /// <summary>
        /// Locks a contract for editing. Returns success if lock is acquired, failure if already locked by another user.
        /// </summary>
        /// <param name="eid">Contract ID to lock</param>
        /// <returns>JSON response indicating success or failure with lock holder information</returns>
        [HttpPost]
        [Authorize(Roles = "Pre-sales,Controls")]
        public async Task<JsonResult> LockContract(string eid)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(eid))
                    return JsonResponse.Fail("Contract ID is required.");

                string currentUserId = User.Identity.GetUserId() ?? "Unknown";
                bool lockAcquired = LockManager.Contracts.TryLock(eid, currentUserId);

                if (lockAcquired)
                {
                    return JsonResponse.Ok("Contract locked successfully.");
                }
                else
                {
                    string lockHolderUserId = LockManager.Contracts.GetLockHolder(eid);
                    string lockHolderUsername = await UserHelper.GetUsernameFromUserId(lockHolderUserId, HttpContext);
                    return JsonResponse.Fail($"This Contract is currently being edited by {lockHolderUsername}");
                }
            }
            catch (Exception ex)
            {
                Logger.Log("LockContract failed: " + ex.ToString());
                return JsonResponse.Fail("An error occurred while locking the contract.");
            }
        }

        /// <summary>
        /// Unlocks a contract after editing. Returns success if lock is released, failure if not locked or held by different user.
        /// </summary>
        /// <param name="eid">Contract ID to unlock</param>
        /// <returns>JSON response indicating success or failure</returns>
        [HttpPost]
        [Authorize(Roles = "Pre-sales,Controls")]
        public JsonResult UnlockContract(string eid)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(eid))
                    return JsonResponse.Fail("Contract ID is required.");

                string currentUserId = User.Identity.GetUserId() ?? "Unknown";
                bool unlocked = LockManager.Contracts.Unlock(eid, currentUserId);

                if (unlocked)
                {
                    return JsonResponse.Ok("Contract unlocked successfully.");
                }
                else
                {
                    return JsonResponse.Fail("Unable to unlock contract. You may not be the lock holder.");
                }
            }
            catch (Exception ex)
            {
                Logger.Log("UnlockContract failed: " + ex.ToString());
                return JsonResponse.Fail("An error occurred while unlocking the contract.");
            }
        }

        /// <summary>
        /// Updates the heartbeat for a locked contract to keep it active.
        /// </summary>
        /// <param name="eid">Contract ID to update heartbeat for</param>
        /// <returns>Success if heartbeat updated, failure if lock not found or not owned by user</returns>
        [HttpPost]
        [Authorize(Roles = "Pre-sales,Controls")]
        public JsonResult HeartbeatLock(string eid)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(eid))
                    return JsonResponse.Fail("Contract ID is required.");

                string currentUserId = User.Identity.GetUserId() ?? "Unknown";
                bool updated = LockManager.Contracts.UpdateHeartbeat(eid, currentUserId);

                if (updated)
                {
                    return JsonResponse.Ok("Heartbeat updated successfully.");
                }
                else
                {
                    return JsonResponse.Fail("Lock not found or not owned by current user.");
                }
            }
            catch (Exception ex)
            {
                Logger.Log("HeartbeatLock failed: " + ex.ToString());
                return JsonResponse.Fail("An error occurred during heartbeat.");
            }
        }

        /// <summary>
        /// Checks if the current user holds the lock for the specified contract
        /// </summary>
        /// <param name="eid">Contract ID to check</param>
        /// <returns>JSON response with lock status</returns>
        [HttpPost]
        [Authorize(Roles = "Pre-sales,Controls")]
        public async Task<JsonResult> CheckLockStatus(string eid)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(eid))
                    return JsonResponse.Fail("Contract ID is required.");

                string currentUserId = User.Identity.GetUserId() ?? "Unknown";
                
                // Clean expired locks first
                LockManager.Contracts.CleanupExpiredLocks();
                
                bool isLockedByCurrentUser = LockManager.Contracts.IsLockedByUser(eid, currentUserId);
                bool isLocked = LockManager.Contracts.IsLocked(eid);
                
                if (isLockedByCurrentUser)
                {
                    return JsonResponse.Ok(new { 
                        hasLock = true, 
                        lockedByCurrentUser = true 
                    }, "Contract is locked by current user");
                }
                else if (isLocked)
                {
                    string lockHolderUserId = LockManager.Contracts.GetLockHolder(eid);
                    string lockHolderUsername = await UserHelper.GetUsernameFromUserId(lockHolderUserId, HttpContext);
                    
                    // For locked by another user, we still return success=true but with hasLock=false
                    // The client-side code checks the Data.hasLock property to determine the action
                    return JsonResponse.Ok(new { 
                        hasLock = false, 
                        lockedByCurrentUser = false,
                        lockHolder = lockHolderUsername
                    }, $"Contract is locked by {lockHolderUsername}");
                }
                else
                {
                    return JsonResponse.Ok(new { 
                        hasLock = false, 
                        lockedByCurrentUser = false 
                    }, "Contract is not locked");
                }
            }
            catch (Exception ex)
            {
                Logger.Log("CheckLockStatus failed: " + ex.ToString());
                return JsonResponse.Fail("An error occurred while checking lock status.");
            }
        }

        #endregion

    }
}