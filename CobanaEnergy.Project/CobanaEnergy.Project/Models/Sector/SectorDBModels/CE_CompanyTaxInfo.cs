using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CobanaEnergy.Project.Models.Sector.SectorDBModels
{
    [Table("CE_CompanyTaxInfo")]
    public class CE_CompanyTaxInfo
    {
        [Key]
        public int CompanyTaxInfoID { get; set; }

        [Required]
        [StringLength(50)]
        public string EntityType { get; set; }

        [Required]
        public int EntityID { get; set; }

        [StringLength(100)]
        public string CompanyRegistration { get; set; }

        [StringLength(100)]
        public string VATNumber { get; set; }

        [StringLength(100)]
        public string Notes { get; set; }

        // Navigation Properties - None needed for polymorphic relationships
        // Relationships are handled manually using EntityType and EntityID
    }
}
  