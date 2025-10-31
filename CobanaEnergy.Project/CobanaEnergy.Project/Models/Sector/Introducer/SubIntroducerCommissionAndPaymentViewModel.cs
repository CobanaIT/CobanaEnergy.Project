using System.ComponentModel.DataAnnotations;

namespace CobanaEnergy.Project.Models.Sector.Introducer
{
    public class SubIntroducerCommissionAndPaymentViewModel
    {
        [Range(0, 100, ErrorMessage = "Commission must be between 0 and 100")]
        [Display(Name = "Sub Introducer Commission (%)")]
        public decimal? SubIntroducerCommission { get; set; }

        [Display(Name = "Sub Introducer Start Date")]
        [DataType(DataType.Date)]
        public string SubIntroducerStartDate { get; set; }

        [Display(Name = "Sub Introducer End Date")]
        [DataType(DataType.Date)]
        public string SubIntroducerEndDate { get; set; }

        [Range(0, 100, ErrorMessage = "Commission must be between 0 and 100")]
        [Display(Name = "Introducer Commission (%)")]
        public decimal? IntroducerCommission { get; set; }

        [Display(Name = "Introducer Start Date")]
        [DataType(DataType.Date)]
        public string IntroducerStartDate { get; set; }

        [Display(Name = "Introducer End Date")]
        [DataType(DataType.Date)]
        public string IntroducerEndDate { get; set; }

        [Display(Name = "Payment Terms")]
        public string PaymentTerms { get; set; }

        [Display(Name = "Commission Type")]
        public string CommissionType { get; set; }
    }
}
