using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CobanaEnergy.Project.Models.Sector.Common;
using CobanaEnergy.Project.Models.Sector.Introducer;

namespace CobanaEnergy.Project.Models.Sector.Introducer
{
    public class SubIntroducerViewModel
    {
        [Required]
        [Display(Name = "Sub Introducer Name")]
        public string SubIntroducerName { get; set; }

        [Display(Name = "Ofgem ID")]
        public string OfgemID { get; set; }

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
        public List<SubIntroducerCommissionAndPaymentViewModel> Commissions { get; set; } = new List<SubIntroducerCommissionAndPaymentViewModel>();
        public BankDetailsViewModel BankDetails { get; set; } = new BankDetailsViewModel();
        public CompanyTaxInfoViewModel CompanyTaxInfo { get; set; } = new CompanyTaxInfoViewModel();

        public SubIntroducerViewModel()
        {
            Commissions.Add(new SubIntroducerCommissionAndPaymentViewModel());
        }
    }
}
