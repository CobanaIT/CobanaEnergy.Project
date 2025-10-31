using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CobanaEnergy.Project.Models.Sector.SectorDBModels
{
    [Table("CE_SubReferralCommissionAndPayment")]
    public class CE_SubReferralCommissionAndPayment
    {
        [Key]
        public int SubReferralCommPayID { get; set; }

        [Required]
        public int SubReferralID { get; set; }

        [Column(TypeName = "decimal")]
        public decimal? SubIntroducerCommission { get; set; }

        public DateTime? SubIntroducerStartDate { get; set; }

        public DateTime? SubIntroducerEndDate { get; set; }

        [Column(TypeName = "decimal")]
        public decimal? IntroducerCommission { get; set; }

        public DateTime? IntroducerStartDate { get; set; }

        public DateTime? IntroducerEndDate { get; set; }

        [StringLength(200)]
        public string PaymentTerms { get; set; }

        [StringLength(100)]
        public string CommissionType { get; set; }

        // Navigation Properties
        [ForeignKey("SubReferralID")]
        public virtual CE_SubReferral SubReferral { get; set; }
    }
}
