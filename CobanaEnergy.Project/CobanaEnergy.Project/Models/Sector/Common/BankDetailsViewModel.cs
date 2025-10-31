using System.ComponentModel.DataAnnotations;

namespace CobanaEnergy.Project.Models.Sector.Common
{
    public class BankDetailsViewModel
    {
        [Display(Name = "Bank Name")]
        public string BankName { get; set; }

        [Display(Name = "Bank Branch Address")]
        public string BankBranchAddress { get; set; }

        [Display(Name = "Receivers Address")]
        public string ReceiversAddress { get; set; }

        [Display(Name = "Account Name")]
        public string AccountName { get; set; }

        [Display(Name = "Account Sort Code")]
        public string AccountSortCode { get; set; }

        [Display(Name = "Account Number")]
        public string AccountNumber { get; set; }

        public string IBAN { get; set; }

        [Display(Name = "Swift Code")]
        public string SwiftCode { get; set; }
    }
}
