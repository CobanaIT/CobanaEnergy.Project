using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.InvoiceSupplierDashboard
{
    [Table("CE_InvoiceSupplierUploads")]
    public class CE_InvoiceSupplierUploads
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