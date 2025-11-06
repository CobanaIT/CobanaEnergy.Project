using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CobanaEnergy.Project.Models.PostSales.RegInvoiceSupplierDashboard.DB_Model
{
    [Table("CE_RegSupplierFileUploads")]
    public class CE_RegSupplierFileUploads
    {
        [Key]
        public long Id { get; set; }

        [ForeignKey("Supplier")]
        public long SupplierId { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; }

        [Required]
        public byte[] FileContent { get; set; }

        [Required]
        [StringLength(250)]
        public string UploadedBy { get; set; }
        public DateTime UploadedOn { get; set; }


        public virtual Supplier.SupplierDBModels.CE_Supplier Supplier { get; set; }
    }
}