using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CobanaEnergy.Project.Models.Sector.SectorDBModels
{
    [Table("CE_BrokerageStaff")]
    public class CE_BrokerageStaff
    {
        [Key]
        public int BrokerageStaffID { get; set; }

        [Required]
        public int SectorID { get; set; }

        [Required]
        [StringLength(200)]
        public string BrokerageStaffName { get; set; }

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

        [StringLength(1000)]
        public string Notes { get; set; }

        // Navigation Properties
        [ForeignKey("SectorID")]
        public virtual CE_Sector Sector { get; set; }
    }
}
