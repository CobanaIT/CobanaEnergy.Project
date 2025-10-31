using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CobanaEnergy.Project.Models.Supplier.SupplierDBModels;

namespace CobanaEnergy.Project.Models.Sector.SectorDBModels
{
    [Table("CE_SectorSupplier")]
    public class CE_SectorSupplier
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SectorId { get; set; }

        [Required]
        public long SupplierId { get; set; }

        // Navigation Properties
        [ForeignKey("SectorId")]
        public virtual CE_Sector Sector { get; set; }

        [ForeignKey("SupplierId")]
        public virtual CE_Supplier Supplier { get; set; }
    }
}
