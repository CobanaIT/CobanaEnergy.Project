using System.ComponentModel.DataAnnotations;

namespace CobanaEnergy.Project.Models.Sector.Commissions
{
    public class IntroducerCommissionAndPaymentViewModel
    {
        public int? Id { get; set; }

        [Required]
        [Range(0, 100, ErrorMessage = "Commission must be between 0 and 100")]
        [Display(Name = "Commission (%)")]
        public decimal Commission { get; set; }

        [Required]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public string StartDate { get; set; }

        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public string EndDate { get; set; }

        [Display(Name = "Payment Terms")]
        public string PaymentTerms { get; set; }

        [Display(Name = "Commission Type")]
        public string CommissionType { get; set; }
    }
}
