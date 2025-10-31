using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CobanaEnergy.Project.Models.Sector.SectorDBModels
{
    [Table("CE_SubBrokerage")]
    public class CE_SubBrokerage
    {
        public CE_SubBrokerage()
        {
            this.CE_SubBrokerageCommissionAndPayments = new List<CE_SubBrokerageCommissionAndPayment>();
        }

        [Key]
        public int SubBrokerageID { get; set; }

        [Required]
        public int SectorID { get; set; }

        [Required]
        [StringLength(200)]
        public string SubBrokerageName { get; set; }

        [StringLength(100)]
        public string OfgemID { get; set; }

        [Required]
        public bool Active { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [StringLength(200)]
        public string Email { get; set; }

        [StringLength(50)]
        public string Landline { get; set; }

        [StringLength(50)]
        public string Mobile { get; set; }

        // Navigation Properties
        [ForeignKey("SectorID")]
        public virtual CE_Sector Sector { get; set; }
        
        public virtual ICollection<CE_SubBrokerageCommissionAndPayment> CE_SubBrokerageCommissionAndPayments { get; set; }
    }
}
