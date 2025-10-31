using System.ComponentModel.DataAnnotations;

namespace CobanaEnergy.Project.Models.Sector.Commissions
{
    public class ReferralPartnerCommissionAndPaymentViewModel
    {
        public int? Id { get; set; }

        [Range(0, 100, ErrorMessage = "Commission must be between 0 and 100")]
        [Display(Name = "Referral Partner Commission (%)")]
        public decimal? ReferralPartnerCommission { get; set; }

        [Display(Name = "Referral Partner Start Date")]
        [DataType(DataType.Date)]
        public string ReferralPartnerStartDate { get; set; }

        [Display(Name = "Referral Partner End Date")]
        [DataType(DataType.Date)]
        public string ReferralPartnerEndDate { get; set; }

        [Range(0, 100, ErrorMessage = "Commission must be between 0 and 100")]
        [Display(Name = "Brokerage Commission (%)")]
        public decimal? BrokerageCommission { get; set; }

        [Display(Name = "Brokerage Start Date")]
        [DataType(DataType.Date)]
        public string BrokerageStartDate { get; set; }

        [Display(Name = "Brokerage End Date")]
        [DataType(DataType.Date)]
        public string BrokerageEndDate { get; set; }

        [Display(Name = "Payment Terms")]
        public string PaymentTerms { get; set; }

        [Display(Name = "Commission Type")]
        public string CommissionType { get; set; }
    }
}
