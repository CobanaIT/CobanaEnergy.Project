using System.ComponentModel.DataAnnotations;

namespace CobanaEnergy.Project.Models.Sector.Brokerage
{
    public class SubBrokerageCommissionAndPaymentViewModel
    {
        [Range(0, 100, ErrorMessage = "Commission must be between 0 and 100")]
        [Display(Name = "Sub Brokerage Commission (%)")]
        public decimal? SubBrokerageCommission { get; set; }

        [Display(Name = "Sub Brokerage Start Date")]
        [DataType(DataType.Date)]
        public string SubBrokerageStartDate { get; set; }

        [Display(Name = "Sub Brokerage End Date")]
        [DataType(DataType.Date)]
        public string SubBrokerageEndDate { get; set; }

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
