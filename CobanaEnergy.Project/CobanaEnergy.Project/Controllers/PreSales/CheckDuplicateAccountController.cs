using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Models;
using Logic;
using Logic.ResponseModel.Helper;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Controllers.PreSales
{
    public class CheckDuplicateAccountController : BaseController
    {
        private readonly ApplicationDBContext db;
        public CheckDuplicateAccountController(ApplicationDBContext _db)
        {
            db = _db;
        }

        [HttpGet]
        [Authorize]
        public async Task<JsonResult> CheckDuplicateAccountUnified(string account)
        {
            if (string.IsNullOrWhiteSpace(account))
                return JsonResponse.Fail("Invalid account number.");

            try
            {
                var electricQuery = db.CE_Accounts
                    .Where(acc => acc.AccountNumber == account)
                    .Join(db.CE_ElectricContracts,
                        acc => acc.EId,
                        elec => elec.EId,
                        (acc, elec) => new
                        {
                            elec.EId,
                            acc.SortCode,
                            acc.AccountNumber,
                            BusinessName = elec.BusinessName,
                            CustomerName = elec.CustomerName,
                            InputDate = elec.InputDate,
                            PreSalesStatus = elec.PreSalesStatus,
                            Duration = elec.Duration,
                            acc.CreatedAt,
                            Type = "Electric",
                            // Fields needed for Agent logic
                            Department = elec.Department,
                            CloserId = elec.CloserId,
                            BrokerageStaffId = elec.BrokerageStaffId,
                            SubIntroducerId = elec.SubIntroducerId
                        });

                var gasQuery = db.CE_Accounts
                    .Where(acc => acc.AccountNumber == account)
                    .Join(db.CE_GasContracts,
                        acc => acc.EId,
                        gas => gas.EId,
                        (acc, gas) => new
                        {
                            gas.EId,
                            acc.SortCode,
                            acc.AccountNumber,
                            BusinessName = gas.BusinessName,
                            CustomerName = gas.CustomerName,
                            InputDate = gas.InputDate,
                            PreSalesStatus = gas.PreSalesStatus,
                            Duration = gas.Duration,
                            acc.CreatedAt,
                            Type = "Gas",
                            // Fields needed for Agent logic
                            Department = gas.Department,
                            CloserId = gas.CloserId,
                            BrokerageStaffId = gas.BrokerageStaffId,
                            SubIntroducerId = gas.SubIntroducerId
                        });

                var rawList = await electricQuery.Concat(gasQuery).ToListAsync();

                var merged = rawList
                    .GroupBy(x => x.EId)
                    .Select(g =>
                    {
                        var elec = g.FirstOrDefault(x => x.Type == "Electric" || x.Type == "Dual");
                        var gas = g.FirstOrDefault(x => x.Type == "Gas" || x.Type == "Dual");

                        bool isDual = elec != null && gas != null;

                        string mergedStatus = isDual
                            ? $"Electric: {elec?.PreSalesStatus}<br>Gas: {gas?.PreSalesStatus}"
                            : elec?.PreSalesStatus ?? gas?.PreSalesStatus ?? "-";

                        string mergedDate = isDual
                            ? $"Electric: {elec?.InputDate}<br>Gas: {gas?.InputDate}"
                            : elec?.InputDate ?? gas?.InputDate ?? "-";

                        DateTime.TryParse(elec?.InputDate, out var elecDate);
                        DateTime.TryParse(gas?.InputDate, out var gasDate);
                        DateTime sortable = elecDate > gasDate ? elecDate : gasDate;

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
                            Agent = agentName,
                            BusinessName = elec?.BusinessName ?? gas?.BusinessName ?? "-",
                            CustomerName = elec?.CustomerName ?? gas?.CustomerName ?? "-",
                            InputDate = mergedDate,
                            PreSalesStatus = mergedStatus,
                            Duration = elec?.Duration ?? gas?.Duration,
                            SortCode = elec?.SortCode ?? gas?.SortCode ?? "-",
                            AccountNumber = elec?.AccountNumber ?? gas?.AccountNumber ?? "-",
                            SortableDate = sortable
                        };
                    })
                    .OrderByDescending(x => x.SortableDate)
                    .ToList();

                return JsonResponse.Ok(merged);
            }
            catch (Exception ex)
            {
                Logger.Log("CheckDuplicateAccountUnified: " + ex);
                return JsonResponse.Fail("Error checking account number.");
            }
        }

        #region Sector Bank Details Duplicate Check

        [HttpGet]
        [Authorize]
        public async Task<JsonResult> CheckDuplicateBankAccount(string account)
        {
            if (string.IsNullOrWhiteSpace(account))
                return JsonResponse.Fail("Invalid account number.");

            try
            {
                var bankDetailsQuery = db.CE_BankDetails
                    .Where(bank => bank.AccountNumber == account)
                    .Select(bank => new
                    {
                        BankName = bank.BankName ?? "-",
                        BankBranchAddress = bank.BankBranchAddress ?? "-",
                        ReceiversAddress = bank.ReceiversAddress ?? "-",
                        AccountName = bank.AccountName ?? "-",
                        AccountSortCode = bank.AccountSortCode ?? "-",
                        AccountNumber = bank.AccountNumber ?? "-",
                        IBAN = bank.IBAN ?? "-",
                        SwiftCode = bank.SwiftCode ?? "-"
                    })
                    .OrderByDescending(x => x.AccountName)
                    .ToListAsync();

                var bankDetails = await bankDetailsQuery;

                if (bankDetails.Count == 0)
                {
                    return JsonResponse.Ok(new List<object>());
                }

                return JsonResponse.Ok(bankDetails);
            }
            catch (Exception ex)
            {
                Logger.Log("CheckDuplicateBankAccount: " + ex);
                return JsonResponse.Fail("Error checking bank account number.");
            }
        }

        #endregion

    }
}