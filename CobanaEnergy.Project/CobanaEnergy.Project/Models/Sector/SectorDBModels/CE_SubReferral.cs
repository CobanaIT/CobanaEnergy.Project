using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CobanaEnergy.Project.Models.Sector.SectorDBModels
{
    [Table("CE_SubReferral")]
    public class CE_SubReferral
    {
        public CE_SubReferral()
        {
            this.CE_SubReferralCommissionAndPayments = new List<CE_SubReferralCommissionAndPayment>();
        }

        [Key]
        public int SubReferralID { get; set; }

        [Required]
        public int SectorID { get; set; }

        [Required]
        [StringLength(200)]
        public string SubReferralPartnerName { get; set; }

        [Required]
        public bool Active { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [StringLength(200)]
        public string SubReferralPartnerEmail { get; set; }

        [StringLength(50)]
        public string SubReferralPartnerLandline { get; set; }

        [StringLength(50)]
        public string SubReferralPartnerMobile { get; set; }

        // Navigation Properties
        [ForeignKey("SectorID")]
        public virtual CE_Sector Sector { get; set; }
        
        public virtual ICollection<CE_SubReferralCommissionAndPayment> CE_SubReferralCommissionAndPayments { get; set; }
    }
}
