using System.ComponentModel.DataAnnotations;

namespace CobanaEnergy.Project.Models.Sector.Commissions
{
    public class LeadGeneratorCommissionAndPaymentViewModel
    {
        public int? Id { get; set; }

        [Range(0, 100, ErrorMessage = "Commission must be between 0 and 100")]
        [Display(Name = "Lead Generator Commission (%)")]
        public decimal? LeadGeneratorCommission { get; set; }

        [Display(Name = "Lead Generator Start Date")]
        [DataType(DataType.Date)]
        public string LeadGeneratorStartDate { get; set; }

        [Display(Name = "Lead Generator End Date")]
        [DataType(DataType.Date)]
        public string LeadGeneratorEndDate { get; set; }

        [Range(0, 100, ErrorMessage = "Commission must be between 0 and 100")]
        [Display(Name = "Closer Commission (%)")]
        public decimal? CloserCommission { get; set; }

        [Display(Name = "Closer Start Date")]
        [DataType(DataType.Date)]
        public string CloserStartDate { get; set; }

        [Display(Name = "Closer End Date")]
        [DataType(DataType.Date)]
        public string CloserEndDate { get; set; }

        [Display(Name = "Payment Terms")]
        public string PaymentTerms { get; set; }

        [Display(Name = "Commission Type")]
        public string CommissionType { get; set; }
    }
}
