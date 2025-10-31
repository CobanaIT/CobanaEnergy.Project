using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CobanaEnergy.Project.Models.Sector.Common;
using CobanaEnergy.Project.Models.Sector.Commissions;
using CobanaEnergy.Project.Models.Sector.Brokerage;
using CobanaEnergy.Project.Models.Sector.ReferralPartner;
using CobanaEnergy.Project.Models.Sector.Introducer;

namespace CobanaEnergy.Project.Models.Sector
{
    public class EditSectorViewModel
    {
        [Required]
        public string SectorId { get; set; }

        [Required]
        [Display(Name = "Sector Type")]
        public string SectorType { get; set; }

        [Required]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Display(Name = "Ofgem ID")]
        public string OfgemID { get; set; }

        [Display(Name = "Active")]
        public bool Active { get; set; }

        [Display(Name = "Start Date")]
        public string StartDate { get; set; }

        [Display(Name = "End Date")]
        public string EndDate { get; set; }

        [Display(Name = "Mobile")]
        public string Mobile { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Landline")]
        public string Landline { get; set; }

        [Display(Name = "Department")]
        public string Department { get; set; }

        // Sector Suppliers (for Brokerage sector type)
        public List<long> SectorSuppliers { get; set; } = new List<long>();

        // Common sections
        public BankDetailsViewModel BankDetails { get; set; } = new BankDetailsViewModel();
        public CompanyTaxInfoViewModel CompanyTaxInfo { get; set; } = new CompanyTaxInfoViewModel();

        // Brokerage specific
        public List<BrokerageCommissionAndPaymentViewModel> BrokerageCommissions { get; set; } = new List<BrokerageCommissionAndPaymentViewModel>();
        public List<BrokerageStaffViewModel> BrokerageStaff { get; set; } = new List<BrokerageStaffViewModel>();
        public List<SubBrokerageViewModel> SubBrokerages { get; set; } = new List<SubBrokerageViewModel>();

        // Closer specific
        public List<CloserCommissionAndPaymentViewModel> CloserCommissions { get; set; } = new List<CloserCommissionAndPaymentViewModel>();

        // Lead Generator specific
        public List<LeadGeneratorCommissionAndPaymentViewModel> LeadGeneratorCommissions { get; set; } = new List<LeadGeneratorCommissionAndPaymentViewModel>();

        // Referral Partner specific
        public List<ReferralPartnerCommissionAndPaymentViewModel> ReferralPartnerCommissions { get; set; } = new List<ReferralPartnerCommissionAndPaymentViewModel>();
        public List<SubReferralViewModel> SubReferrals { get; set; } = new List<SubReferralViewModel>();

        // Introducer specific
        public List<IntroducerCommissionAndPaymentViewModel> IntroducerCommissions { get; set; } = new List<IntroducerCommissionAndPaymentViewModel>();
        public List<SubIntroducerViewModel> SubIntroducers { get; set; } = new List<SubIntroducerViewModel>();

        public EditSectorViewModel()
        {
            // Initialize with at least one item for each list
            BrokerageCommissions.Add(new BrokerageCommissionAndPaymentViewModel());
            BrokerageStaff.Add(new BrokerageStaffViewModel());
            SubBrokerages.Add(new SubBrokerageViewModel());
            CloserCommissions.Add(new CloserCommissionAndPaymentViewModel());
            LeadGeneratorCommissions.Add(new LeadGeneratorCommissionAndPaymentViewModel());
            ReferralPartnerCommissions.Add(new ReferralPartnerCommissionAndPaymentViewModel());
            SubReferrals.Add(new SubReferralViewModel());
            IntroducerCommissions.Add(new IntroducerCommissionAndPaymentViewModel());
            SubIntroducers.Add(new SubIntroducerViewModel());
        }
    }
}
