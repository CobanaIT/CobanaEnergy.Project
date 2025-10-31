using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CobanaEnergy.Project.Models.Sector.SectorDBModels
{
    [Table("CE_SubIntroducerCommissionAndPayment")]
    public class CE_SubIntroducerCommissionAndPayment
    {
        [Key]
        public int SubIntroducerCommPayID { get; set; }

        [Required]
        public int SubIntroducerID { get; set; }

        [Column(TypeName = "decimal")]
        public decimal? SubIntroducerCommission { get; set; }

        public DateTime? SubIntroducerCommissionStartDate { get; set; }

        public DateTime? SubIntroducerCommissionEndDate { get; set; }

        [Column(TypeName = "decimal")]
        public decimal? IntroducerCommission { get; set; }

        public DateTime? IntroducerCommissionStartDate { get; set; }

        public DateTime? IntroducerCommissionEndDate { get; set; }

        [StringLength(200)]
        public string PaymentTerms { get; set; }

        [StringLength(100)]
        public string CommissionType { get; set; }

        // Navigation Properties
        [ForeignKey("SubIntroducerID")]
        public virtual CE_SubIntroducer SubIntroducer { get; set; }
    }
}
