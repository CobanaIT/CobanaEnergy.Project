using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CobanaEnergy.Project.Models.Sector.SectorDBModels
{
    [Table("CE_ReferralPartnerCommissionAndPayment")]
    public class CE_ReferralPartnerCommissionAndPayment
    {
        [Key]
        public int ReferralCommPayID { get; set; }

        [Required]
        public int SectorID { get; set; }

        [Column(TypeName = "decimal")]
        public decimal? ReferralPartnerCommission { get; set; }

        public DateTime? ReferralPartnerStartDate { get; set; }

        public DateTime? ReferralPartnerEndDate { get; set; }

        [Column(TypeName = "decimal")]
        public decimal? BrokerageCommission { get; set; }

        public DateTime? BrokerageStartDate { get; set; }

        public DateTime? BrokerageEndDate { get; set; }

        [StringLength(200)]
        public string PaymentTerms { get; set; }

        [StringLength(100)]
        public string CommissionType { get; set; }

        // Navigation Properties
        [ForeignKey("SectorID")]
        public virtual CE_Sector Sector { get; set; }
    }
}
