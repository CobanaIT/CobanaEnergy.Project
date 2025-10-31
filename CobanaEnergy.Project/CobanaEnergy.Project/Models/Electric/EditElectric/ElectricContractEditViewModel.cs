using CobanaEnergy.Project.Models.Supplier.SupplierSnapshots;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Controllers.PreSales
{
    public class ElectricContractEditViewModel
    {
        [Required]
        public string EId { get; set; }

        // Removed: Agent, Introducer, SubIntroducer fields as per requirements

        [Required(ErrorMessage = "Top Line is required.")]
        [StringLength(9, MinimumLength = 8, ErrorMessage = "Top Line must be between 8 and 9 characters.")]
        [RegularExpression("^[a-zA-Z0-9]*$", ErrorMessage = "Top Line must be alphanumeric only.")]
        public string TopLine { get; set; }

        [Required(ErrorMessage = "MPAN is required.")]
        [RegularExpression(@"^\d{13}$|^N/A$", ErrorMessage = "MPAN must be either 13 digits or 'N/A'.")]
        public string MPAN { get; set; }
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

        [Required(ErrorMessage = "Phone number is required.")]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "Phone number must be exactly 11 digits.")]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "Phone number must contain only digits.")]
        public string PhoneNumber1 { get; set; }

        public string PhoneNumber2 { get; set; }

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string EmailAddress { get; set; }
        public string InputDate { get; set; }
        [Required]
        public string InitialStartDate { get; set; }

        [Required(ErrorMessage = "Uplift is required")]
        [Range(0.00, 100.00, ErrorMessage = "Uplift must be a valid decimal between 0.00 and 100")]
        public decimal Uplift { get; set; }

        [Required(ErrorMessage = "Duration is required.")]
        [Range(1, 10, ErrorMessage = "Duration must be between 1 and 10 years.")]
        public string Duration { get; set; }

        [Required(ErrorMessage = "InputEAC is required")]
        //[Range(0.00, 100.00, ErrorMessage = "InputEAC must be a valid decimal between 0.00 and 100")]
        public decimal InputEAC { get; set; }

        [Required(ErrorMessage = "DayRate is required")]
        [Range(0.00, 100.00, ErrorMessage = "DayRate must be a valid decimal between 0.00 and 100")]
        public decimal DayRate { get; set; }

        [Required(ErrorMessage = "NightRate is required")]
        [Range(0.00, 100.00, ErrorMessage = "NightRate must be a valid decimal between 0.00 and 100")]
        public decimal NightRate { get; set; }

        [Required(ErrorMessage = "EveWeekendRate is required")]
        [Range(0.00, 100.00, ErrorMessage = "EveWeekendRate must be a valid decimal between 0.00 and 100")]
        public decimal EveWeekendRate { get; set; }

        [Required(ErrorMessage = "OtherRate is required")]
        [Range(0.00, 100.00, ErrorMessage = "OtherRate must be a valid decimal between 0.00 and 100")]
        public decimal OtherRate { get; set; }

        [Required(ErrorMessage = "StandingCharge is required")]
        //[Range(0.00, 100.00, ErrorMessage = "StandingCharge must be a valid decimal between 0.00 and 100")]
        public decimal StandingCharge { get; set; }

        [Required(ErrorMessage = "Sort Code is required.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Sort Code must be exactly 6 digits.")]
        public string SortCode { get; set; }

        [Required(ErrorMessage = "Account Number is required.")]
        [RegularExpression(@"^\d{8}$", ErrorMessage = "Account Number must be exactly 8 digits.")]
        public string AccountNumber { get; set; }
        [Required]
        public string CurrentSupplier { get; set; }
        [Required]
        public long SupplierId { get; set; }
        [Required]
        public long ProductId { get; set; }
        public string EMProcessor { get; set; } 
        public bool ContractChecked { get; set; }
        public bool ContractAudited { get; set; }
        public bool Terminated { get; set; }
        public string ContractNotes { get; set; }
        public string LOGS { get; set; }
        public string CreatedAt { get; set; }
        public string UpdatedAt { get; set; }
        public string Type { get; set; } = "Electric";
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
        public string SalesType { get; set; }
        [Required]
        public string SalesTypeStatus { get; set; }
        [Required]
        public string SupplierCommsType { get; set; }
        [Required]
        public string PreSalesStatus { get; set; }
        
        public string PreSalesFollowUpDate { get; set; }
        
        public ElectricSupplierSnapshotViewModel SupplierSnapshot { get; set; }

        // Brokerage Details
        [Required(ErrorMessage = "Brokerage is required")]
        public int BrokerageId { get; set; }
        
        public string OfgemId { get; set; }
    }
}