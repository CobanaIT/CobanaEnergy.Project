using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CobanaEnergy.Project.Models.Sector.SectorDBModels
{
    [Table("CE_IntroducerCommissionAndPayment")]
    public class CE_IntroducerCommissionAndPayment
    {
        [Key]
        public int CommissionID { get; set; }

        [Required]
        public int SectorID { get; set; }

        [Required]
        [Column(TypeName = "decimal")]
        public decimal CommissionPercent { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [StringLength(200)]
        public string PaymentTerms { get; set; }

        [StringLength(100)]
        public string CommissionType { get; set; }

        // Navigation Property
        [ForeignKey("SectorID")]
        public virtual CE_Sector CE_Sector { get; set; }
    }
}
