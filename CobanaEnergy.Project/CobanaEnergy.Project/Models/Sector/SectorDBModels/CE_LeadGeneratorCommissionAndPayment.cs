using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CobanaEnergy.Project.Models.Sector.SectorDBModels
{
    [Table("CE_LeadGeneratorCommissionAndPayment")]
    public class CE_LeadGeneratorCommissionAndPayment
    {
        [Key]
        public int LeadGenCommPayID { get; set; }

        [Required]
        public int SectorID { get; set; }

        [Column(TypeName = "decimal")]
        public decimal? LeadGeneratorCommissionPercent { get; set; }

        public DateTime? LeadGeneratorStartDate { get; set; }

        public DateTime? LeadGeneratorEndDate { get; set; }

        [Column(TypeName = "decimal")]
        public decimal? CloserCommissionPercent { get; set; }

        public DateTime? CloserStartDate { get; set; }

        public DateTime? CloserEndDate { get; set; }

        [StringLength(200)]
        public string PaymentTerms { get; set; }

        [StringLength(100)]
        public string CommissionType { get; set; }

        // Navigation Properties
        [ForeignKey("SectorID")]
        public virtual CE_Sector Sector { get; set; }
    }
}
