using CobanaEnergy.Project.Models.Accounts.SuppliersModels.BGB.DBModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Models.Accounts.SuppliersModels.BGB
{
    public class EditBGBContractViewModel
    {
        public string Id { get; set; }
        public string SupplierId { get; set; }

        public string InvoiceNo { get; set; }
        public string InvoiceDate { get; set; } 
        public string PaymentDate { get; set; }

        // Common
        public string Department { get; set; }
        public string BusinessName { get; set; }

        // Department-based fields
        // For Brokers
        public string BrokerageStaffName { get; set; }
        public string SubBrokerageName { get; set; }

        // For InHouse
        public string CloserName { get; set; }
        public string LeadGeneratorName { get; set; }
        public string ReferralPartnerName { get; set; }
        public string SubReferralPartnerName { get; set; }

        // For Introducers
        public string IntroducerName { get; set; }
        public string SubIntroducerName { get; set; }

        // Electric
        public string SalesTypeElectric { get; set; }
        public string MPAN { get; set; }
        public string DurationElectric { get; set; }
        public string InputDateElectric { get; set; }

        // Gas
        public string SalesTypeGas { get; set; }
        public string MPRN { get; set; }
        public string DurationGas { get; set; }
        public string InputDateGas { get; set; }

        public string ContractNotes { get; set; }

        public bool HasElectricDetails { get; set; } = false;
        public bool HasGasDetails { get; set; } = false;

        public string UpliftElectric { get; set; }
        public List<SelectListItem> ProductElectricList { get; set; } = new List<SelectListItem>();
        public long? SelectedProductElectric { get; set; }
        public string SupplierCommsTypeElectric { get; set; }
        public string CommissionElectric { get; set; }

        public List<SelectListItem> ProductGasList { get; set; } = new List<SelectListItem>();
        public long? SelectedProductGas { get; set; }
        public string SupplierCommsTypeGas { get; set; }
        public string CommissionGas { get; set; }
        public string UpliftGas { get; set; }

        public string InitialStartDate { get; set; }
        public string CED { get; set; }

        public string OtherAmount { get; set; }
        public string CedCOT { get; set; }
        public string CotLostConsumption { get; set; }
        public string CobanaDueCommission { get; set; }
        public string CobanaFinalReconciliation { get; set; }
        public string CommissionFollowUpDate { get; set; }
        public string SupplierCobanaInvoiceNotes { get; set; }

        // Metrics
        public string ContractDurationDays { get; set; }
        public string LiveDays { get; set; }
        public string PercentLiveDays { get; set; }
        public string TotalCommissionForecast { get; set; }
        public string InitialCommissionForecast { get; set; }
        public string COTLostReconciliation { get; set; }
        public string TotalAverageEAC { get; set; }
        public string PaymentStatus { get; set; }
        public string ContractStatus { get; set; }
        public List<CE_PaymentAndNoteLogs> PaymentNoteLogs { get; set; }
    }
}