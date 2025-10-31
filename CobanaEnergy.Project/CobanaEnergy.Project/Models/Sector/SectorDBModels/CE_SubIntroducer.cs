using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CobanaEnergy.Project.Models.Sector.SectorDBModels
{
    [Table("CE_SubIntroducer")]
    public class CE_SubIntroducer
    {
        public CE_SubIntroducer()
        {
            this.CE_SubIntroducerCommissionAndPayments = new List<CE_SubIntroducerCommissionAndPayment>();
        }

        [Key]
        public int SubIntroducerID { get; set; }

        [Required]
        public int SectorID { get; set; }

        [Required]
        [StringLength(200)]
        public string SubIntroducerName { get; set; }

        [StringLength(50)]
        public string OfgemID { get; set; }

        [Required]
        public bool Active { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [StringLength(200)]
        public string SubIntroducerEmail { get; set; }

        [StringLength(50)]
        public string SubIntroducerLandline { get; set; }

        [StringLength(50)]
        public string SubIntroducerMobile { get; set; }

        // Navigation Properties
        [ForeignKey("SectorID")]
        public virtual CE_Sector Sector { get; set; }
        
        public virtual ICollection<CE_SubIntroducerCommissionAndPayment> CE_SubIntroducerCommissionAndPayments { get; set; }
    }
}
