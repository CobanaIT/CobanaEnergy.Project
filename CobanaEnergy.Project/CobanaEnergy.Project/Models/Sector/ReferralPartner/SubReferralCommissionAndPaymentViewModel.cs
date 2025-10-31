using System.ComponentModel.DataAnnotations;

namespace CobanaEnergy.Project.Models.Sector.ReferralPartner
{
    public class SubReferralCommissionAndPaymentViewModel
    {
        [Range(0, 100, ErrorMessage = "Commission must be between 0 and 100")]
        [Display(Name = "Sub Referral Commission (%)")]
        public decimal? SubReferralCommission { get; set; }

        [Display(Name = "Sub Referral Start Date")]
        [DataType(DataType.Date)]
        public string SubReferralStartDate { get; set; }

        [Display(Name = "Sub Referral End Date")]
        [DataType(DataType.Date)]
        public string SubReferralEndDate { get; set; }

        [Range(0, 100, ErrorMessage = "Commission must be between 0 and 100")]
        [Display(Name = "Referral Partner Commission (%)")]
        public decimal? ReferralPartnerCommission { get; set; }

        [Display(Name = "Referral Partner Start Date")]
        [DataType(DataType.Date)]
        public string ReferralPartnerStartDate { get; set; }

        [Display(Name = "Referral Partner End Date")]
        [DataType(DataType.Date)]
        public string ReferralPartnerEndDate { get; set; }

        [Display(Name = "Payment Terms")]
        public string PaymentTerms { get; set; }

        [Display(Name = "Commission Type")]
        public string CommissionType { get; set; }
    }
}
