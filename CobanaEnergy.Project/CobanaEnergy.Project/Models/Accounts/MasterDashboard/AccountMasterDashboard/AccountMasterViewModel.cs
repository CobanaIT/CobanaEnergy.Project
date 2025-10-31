using CobanaEnergy.Project.Models.Accounts.ProblematicsDashboard;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Models.Accounts.MasterDashboard.AccountMasterDashboard
{

    public class AccountMasterDashboardViewModel
    {
        public long? SupplierId { get; set; }
        public List<SelectListItem> Suppliers { get; set; } = new List<SelectListItem>();
        public List<AccountMasterRowViewModel> AccountsContracts { get; set; } = new List<AccountMasterRowViewModel>();
    }


    public class AccountMasterRowViewModel
    {
        public long ContractId { get; set; }
        public string EId { get; set; }
        public long SupplierId { get; set; }
        public string SupplierName { get; set; }
        public string BusinessName { get; set; }
        public string MPXN { get; set; }
        public string InputEAC { get; set; } 
        public string SupplierEAC { get; set; }
        public string InputDate { get; set; }
        public string StartDate { get; set; }
        public string CED { get; set; }
        public string COTDate { get; set; }
        public string ContractStatus { get; set; }
        public string PaymentStatus { get; set; }
        public string PreviousInvoiceNumbers { get; set; } // comma separated
        public string AccountsNotes { get; set; }
        public string CommissionForecast { get; set; }
        public string CobanaDueCommission { get; set; }
        public decimal? CobanaPaidCommission { get; set; }
        public string ContractType { get; set; }
        public string CobanaFinalReconciliation { get; set; }
        /// <summary>
        /// Action and Controller 
        /// </summary>
        public string Controller { get; set; }
        public string Action { get; set; }

    }

    public class ContractViewModel
    {
        public long Id { get; set; }
        public string EId { get; set; }
        public string MPXN { get; set; }
        public string ContractType { get; set; }
        public string BusinessName { get; set; }
        public string CustomerName { get; set; }
        public string BusinessDoorNumber { get; set; }
        public string BusinessHouseName { get; set; }
        public string BusinessStreet { get; set; }
        public string CampaignName { get; set; }
        public string BusinessTown { get; set; }
        public string BusinessCounty { get; set; }
        public string PhoneNumber1 { get; set; }
        public string PhoneNumber2 { get; set; }


        [Required(ErrorMessage = "Uplift is required")]
        [Range(0.00, 100.00, ErrorMessage = "Uplift must be a valid decimal between 0.00 and 100")]
        public string Uplift { get; set; }

        [Required(ErrorMessage = "Duration is required.")]
        [Range(1, 10, ErrorMessage = "Duration must be between 1 and 10 years.")]
        public string Duration { get; set; }

        [Required(ErrorMessage = "InputEAC is required")]
        // [Range(0.00, 100.00, ErrorMessage = "InputEAC must be a valid decimal between 0.00 and 100")]
        public string InputEAC { get; set; }

        [Required(ErrorMessage = "UnitRate is required")]
        [Range(0.00, 100.00, ErrorMessage = "UnitRate must be a valid decimal between 0.00 and 100")]
        public string UnitRate { get; set; }

        [Required(ErrorMessage = "OtherRate is required")]
        [Range(0.00, 100.00, ErrorMessage = "OtherRate must be a valid decimal between 0.00 and 100")]
        public string OtherRate { get; set; }

        [Required(ErrorMessage = "StandingCharge is required")]
        //[Range(0.00, 100.00, ErrorMessage = "StandingCharge must be a valid decimal between 0.00 and 100")]
        public string StandingCharge { get; set; }

        [Required]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Sort Code must be exactly 6 digits.")]
        public string SortCode { get; set; }

        [Required]
        [RegularExpression(@"^\d{8}$", ErrorMessage = "Account Number must be exactly 8 digits.")]
        public string AccountNumber { get; set; }

        public string CurrentSupplier { get; set; }
        public string EMProcessor { get; set; }

        public string Source { get; set; }
        public string SalesType { get; set; }
        public string SalesTypeStatus { get; set; }
        public string PreSalesStatus { get; set; }
        public string TopLine { get; set; }
        public string PreSalesFollowUpDate { get; set; }
        public string OfgemId { get; set; }
        public string Collaboration { get; set; }

        public bool ContractChecked { get; set; }
        public bool ContractAudited { get; set; }
        public bool Terminated { get; set; }




        public string InputDate { get; set; }
        public string ContractNotes { get; set; }
        public string StartDate { get; set; }
        public long SupplierId { get; set; }
        public string SupplierName { get; set; }
        public string Department { get; set; }
        public int? BrokerageId { get; set; }
        public int? BrokerageStaffId { get; set; }
        public int? SubBrokerageId { get; set; }
        public int? CloserId { get; set; }
        public int? LeadGeneratorId { get; set; }
        public string EmailAddress { get; set; }
        public string PostCode { get; set; }
        public int? ReferralPartnerId { get; set; }
        public int? SubReferralPartnerId { get; set; }
        public int? IntroducerId { get; set; }
        public int? SubIntroducerId { get; set; }

        // Reconciliation
        public long? ReconciliationId { get; set; }
        public string CED { get; set; }
        public string COTDate { get; set; }
        public string CobanaDueCommission { get; set; }
        public string CobanaFinalReconciliation { get; set; }

        // Metrics
        public string ContractDurationDays { get; set; }
        public string LiveDays { get; set; }
        public string PercentLiveDays { get; set; }
        public string TotalCommissionForecast { get; set; }
        public string InitialCommissionForecast { get; set; }
        public string TotalAverageEAC { get; set; }
        public string Link { get; set; }
        public string SupplierCommsType { get; set; }
        public long ProductId { get; set; }
    }
}