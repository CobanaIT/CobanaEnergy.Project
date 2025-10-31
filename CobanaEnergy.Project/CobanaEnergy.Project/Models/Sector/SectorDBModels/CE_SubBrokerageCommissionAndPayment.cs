using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CobanaEnergy.Project.Models.Sector.SectorDBModels
{
    [Table("CE_SubBrokerageCommissionAndPayment")]
    public class CE_SubBrokerageCommissionAndPayment
    {
        [Key]
        public int SubBrokerageCommPayID { get; set; }

        [Required]
        public int SubBrokerageID { get; set; }

        [Column(TypeName = "decimal")]
        public decimal? SubBrokerageCommissionPercent { get; set; }

        public DateTime? SubBrokerageStartDate { get; set; }

        public DateTime? SubBrokerageEndDate { get; set; }

        [Column(TypeName = "decimal")]
        public decimal? BrokerageCommissionPercent { get; set; }

        public DateTime? BrokerageStartDate { get; set; }

        public DateTime? BrokerageEndDate { get; set; }

        [StringLength(200)]
        public string PaymentTerms { get; set; }

        [StringLength(100)]
        public string CommissionType { get; set; }

        // Navigation Properties
        [ForeignKey("SubBrokerageID")]
        public virtual CE_SubBrokerage SubBrokerage { get; set; }
    }
}
