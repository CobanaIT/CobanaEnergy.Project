using System.ComponentModel.DataAnnotations;

namespace CobanaEnergy.Project.Models.Sector.Common
{
    public class CompanyTaxInfoViewModel
    {
        [Display(Name = "Company Registration")]
        public string CompanyRegistration { get; set; }

        [Display(Name = "VAT Number")]
        public string VATNumber { get; set; }

        public string Notes { get; set; }
    }
}
