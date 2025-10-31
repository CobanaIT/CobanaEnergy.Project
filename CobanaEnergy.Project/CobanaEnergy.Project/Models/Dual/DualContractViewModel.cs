using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Dual
{
    public class DualContractCreateViewModel
    {
        public string EId { get; set; }
        // Site Details
        [Required]
        public string Department { get; set; }
        
        // New dynamic fields based on Department
        public int? CloserId { get; set; }
        public int? ReferralPartnerId { get; set; }
        public int? SubReferralPartnerId { get; set; }
        public int? BrokerageStaffId { get; set; }
        public int? IntroducerId { get; set; }
        public int? SubIntroducerId { get; set; }
        public int? SubBrokerageId { get; set; }
        public string Collaboration { get; set; }
        public int? LeadGeneratorId { get; set; }

        [Required]
        public string Source { get; set; }

        [Required]
        public string ElectricSalesType { get; set; }

        [Required]
        public string GasSalesType { get; set; }

        [Required]
        public string ElectricSalesTypeStatus { get; set; }

        [Required]
        public string GasSalesTypeStatus { get; set; }

        // Supply & Product
        [Required(ErrorMessage = "Top Line is required.")]
        [StringLength(9, MinimumLength = 8, ErrorMessage = "Top Line must be between 8 and 9 characters.")]
        [RegularExpression("^[a-zA-Z0-9]*$", ErrorMessage = "Top Line must be alphanumeric.")]
        public string TopLine { get; set; }

        [Required(ErrorMessage = "MPAN is required.")]
        [RegularExpression(@"^\d{13}$|^N/A$", ErrorMessage = "MPAN must be 13 digits or 'N/A'.")]
        public string MPAN { get; set; }

        [Required(ErrorMessage = "MPRN is required.")]
        [RegularExpression(@"^\d{6,10}$|^N/A$", ErrorMessage = "MPRN must be 6–10 digits or 'N/A'.")]
        public string MPRN { get; set; }

        [Required]
        public string ElectricCurrentSupplier { get; set; }

        [Required]
        public string GasCurrentSupplier { get; set; }

        [Required]
        public long ElectricSupplierId { get; set; }

        [Required]
        public long ElectricProductId { get; set; }

        [Required]
        public long GasSupplierId { get; set; }

        [Required]
        public long GasProductId { get; set; }

        [Required]
        public string ElectricSupplierCommsType { get; set; }

        [Required]
        public string GasSupplierCommsType { get; set; }

        // Business Location
        [Required]
        public string BusinessName { get; set; }

        [Required]
        public string CustomerName { get; set; }

        [Required]
        public string BusinessDoorNumber { get; set; }
        
        public string BusinessHouseName { get; set; }

        [Required]
        public string BusinessStreet { get; set; }

        [Required]
        public string BusinessTown { get; set; }

        public string BusinessCounty { get; set; }

        [Required]
        public string PostCode { get; set; }

        // Contact
        [Required]
        [StringLength(11, MinimumLength = 11)]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "Must be 11 digits.")]
        public string PhoneNumber1 { get; set; }

        public string PhoneNumber2 { get; set; }

        [Required]
        [EmailAddress]
        public string EmailAddress { get; set; }

        // Timeline
        public string InputDate { get; set; }

        [Required]
        public string ElectricInitialStartDate { get; set; }

        [Required]
        public string GasInitialStartDate { get; set; }

        [Required(ErrorMessage = "Electric Duration required.")]
        public string ElectricDuration { get; set; }

        [Required(ErrorMessage = "Gas Duration required.")]
        [Range(1, 10, ErrorMessage = "Gas Duration must be 1–10 years.")]
        public string GasDuration { get; set; }

        // Pricing Electric
        [Required(ErrorMessage = "Electric Uplift is required")]
        [Range(0.00, 100.00, ErrorMessage = "Electric Uplift must be a valid decimal between 0.00 and 100")]
        public decimal ElectricUplift { get; set; }

        [Required(ErrorMessage = "Electric InputEAC is required")]
        //[Range(0.00, 100.00, ErrorMessage = "Electric InputEAC must be a valid decimal between 0.00 and 100")]
        public decimal ElectricInputEAC { get; set; }

        [Required(ErrorMessage = "Electric Day Rate is required")]
        [Range(0.00, 100.00, ErrorMessage = "Electric Day Rate must be a valid decimal between 0.00 and 100")]
        public decimal ElectricDayRate { get; set; }

        [Required(ErrorMessage = "Electric Night Rate is required")]
        [Range(0.00, 100.00, ErrorMessage = "Electric Night Rate must be a valid decimal between 0.00 and 100")]
        public decimal ElectricNightRate { get; set; }

        [Required(ErrorMessage = "Electric Evening/Weekend Rate is required")]
        [Range(0.00, 100.00, ErrorMessage = "Electric Evening/Weekend Rate must be a valid decimal between 0.00 and 100")]
        public decimal ElectricEveWeekendRate { get; set; }

        [Required(ErrorMessage = "Electric Other Rate is required")]
        [Range(0.00, 100.00, ErrorMessage = "Electric Other Rate must be a valid decimal between 0.00 and 100")]
        public decimal ElectricOtherRate { get; set; }

        [Required(ErrorMessage = "Electric Standing Charge is required")]
        public decimal ElectricStandingCharge { get; set; }

        [Required(ErrorMessage = "Gas Uplift is required")]
        [Range(0.00, 100.00, ErrorMessage = "Gas Uplift must be a valid decimal between 0.00 and 100")]
        public decimal GasUplift { get; set; }

        [Required(ErrorMessage = "Gas InputEAC is required")]
       // [Range(0.00, 100.00, ErrorMessage = "Gas InputEAC must be a valid decimal between 0.00 and 100")]
        public decimal GasInputEAC { get; set; }

        [Required(ErrorMessage = "Gas Unit Rate is required")]
        [Range(0.00, 100.00, ErrorMessage = "Gas Unit Rate must be a valid decimal between 0.00 and 100")]
        public decimal GasUnitRate { get; set; }

        [Required(ErrorMessage = "Gas Other Rate is required")]
        [Range(0.00, 100.00, ErrorMessage = "Gas Other Rate must be a valid decimal between 0.00 and 100")]
        public decimal GasOtherRate { get; set; }

        [Required(ErrorMessage = "Gas Standing Charge is required")]
        public decimal GasStandingCharge { get; set; }

        [Required]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Sort Code must be 6 digits.")]
        public string SortCode { get; set; }

        [Required]
        [RegularExpression(@"^\d{8}$", ErrorMessage = "Account Number must be 8 digits.")]
        public string AccountNumber { get; set; }

        [Required]
        public string ElectricPreSalesStatus { get; set; }
        
        public string ElectricPreSalesFollowUpDate { get; set; }

        [Required]
        public string GasPreSalesStatus { get; set; }
        
        public string GasPreSalesFollowUpDate { get; set; }

        public string EMProcessor { get; set; }

        public bool ContractChecked { get; set; }

        public bool ContractAudited { get; set; }

        public bool Terminated { get; set; }

        public string ContractNotes { get; set; }

        public string CreatedAt { get; set; }

        public string UpdatedAt { get; set; }

        public string Type { get; set; } = "Dual";

        // Brokerage Details
        [Required(ErrorMessage = "Brokerage is required")]
        public int BrokerageId { get; set; }
        
        public string OfgemId { get; set; }
    }
}