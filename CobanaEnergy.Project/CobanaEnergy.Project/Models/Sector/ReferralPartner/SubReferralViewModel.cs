using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CobanaEnergy.Project.Models.Sector.Common;
using CobanaEnergy.Project.Models.Sector.ReferralPartner;

namespace CobanaEnergy.Project.Models.Sector.ReferralPartner
{
    public class SubReferralViewModel
    {
        [Required]
        [Display(Name = "Sub Referral Partner Name")]
        public string SubReferralPartnerName { get; set; }

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

        // Sub-section specific collections
        public List<SubReferralCommissionAndPaymentViewModel> Commissions { get; set; } = new List<SubReferralCommissionAndPaymentViewModel>();
        public BankDetailsViewModel BankDetails { get; set; } = new BankDetailsViewModel();
        public CompanyTaxInfoViewModel CompanyTaxInfo { get; set; } = new CompanyTaxInfoViewModel();

        public SubReferralViewModel()
        {
            Commissions.Add(new SubReferralCommissionAndPaymentViewModel());
        }
    }
}
