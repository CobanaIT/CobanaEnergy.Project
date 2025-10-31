using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CobanaEnergy.Project.Models.Sector.SectorDBModels
{
    [Table("CE_BrokerageCommissionAndPayment")]
    public class CE_BrokerageCommissionAndPayment
    {
        [Key]
        public int CommissionID { get; set; }

        [Required]
        public int SectorID { get; set; }

        [Required]
        [Column(TypeName = "decimal")]
        public decimal Commission { get; set; }

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
