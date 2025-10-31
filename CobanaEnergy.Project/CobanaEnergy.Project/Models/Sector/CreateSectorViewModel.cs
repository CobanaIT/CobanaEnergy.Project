using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CobanaEnergy.Project.Models.Sector.Common;
using CobanaEnergy.Project.Models.Sector.Commissions;
using CobanaEnergy.Project.Models.Sector.Brokerage;
using CobanaEnergy.Project.Models.Sector.ReferralPartner;
using CobanaEnergy.Project.Models.Sector.Introducer;

namespace CobanaEnergy.Project.Models.Sector
{
    public class CreateSectorViewModel
    {
        [Required]
        [Display(Name = "Sector Type")]
        public string SectorType { get; set; }

        // Sector Details (Generic)
        [Required]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Active")]
        public bool Active { get; set; }

        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public string StartDate { get; set; }

        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public string EndDate { get; set; }

        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Landline")]
        public string Landline { get; set; }

        [Display(Name = "Mobile")]
        public string Mobile { get; set; }

        [Display(Name = "Ofgem ID")]
        public string OfgemID { get; set; }

        [Display(Name = "Department")]
        public string Department { get; set; }

        // Sector Suppliers (for Brokerage sector type)
        public List<long> SectorSuppliers { get; set; } = new List<long>();

        // Bank Details (Generic)
        public BankDetailsViewModel BankDetails { get; set; } = new BankDetailsViewModel();

        // Company Tax Info (Generic)
        public CompanyTaxInfoViewModel CompanyTaxInfo { get; set; } = new CompanyTaxInfoViewModel();

        // Commission and Payment Collections
        public List<BrokerageCommissionAndPaymentViewModel> BrokerageCommissions { get; set; } = new List<BrokerageCommissionAndPaymentViewModel>();
        public List<CloserCommissionAndPaymentViewModel> CloserCommissions { get; set; } = new List<CloserCommissionAndPaymentViewModel>();
        public List<IntroducerCommissionAndPaymentViewModel> IntroducerCommissions { get; set; } = new List<IntroducerCommissionAndPaymentViewModel>();
        public List<ReferralPartnerCommissionAndPaymentViewModel> ReferralPartnerCommissions { get; set; } = new List<ReferralPartnerCommissionAndPaymentViewModel>();
        public List<LeadGeneratorCommissionAndPaymentViewModel> LeadGeneratorCommissions { get; set; } = new List<LeadGeneratorCommissionAndPaymentViewModel>();

        // Staff and Sub-sections Collections
        public List<BrokerageStaffViewModel> BrokerageStaff { get; set; } = new List<BrokerageStaffViewModel>();
        public List<SubBrokerageViewModel> SubBrokerages { get; set; } = new List<SubBrokerageViewModel>();
        public List<SubReferralViewModel> SubReferrals { get; set; } = new List<SubReferralViewModel>();
        public List<SubIntroducerViewModel> SubIntroducers { get; set; } = new List<SubIntroducerViewModel>();

        public CreateSectorViewModel()
        {
            // Initialize with at least one item for each collection
            BrokerageCommissions.Add(new BrokerageCommissionAndPaymentViewModel());
            CloserCommissions.Add(new CloserCommissionAndPaymentViewModel());
            IntroducerCommissions.Add(new IntroducerCommissionAndPaymentViewModel());
            ReferralPartnerCommissions.Add(new ReferralPartnerCommissionAndPaymentViewModel());
            LeadGeneratorCommissions.Add(new LeadGeneratorCommissionAndPaymentViewModel());
            
            BrokerageStaff.Add(new BrokerageStaffViewModel());
            SubBrokerages.Add(new SubBrokerageViewModel());
            SubReferrals.Add(new SubReferralViewModel());
            SubIntroducers.Add(new SubIntroducerViewModel());
        }
    }
}
