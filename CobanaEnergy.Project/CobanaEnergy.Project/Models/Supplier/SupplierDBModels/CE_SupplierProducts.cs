using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Supplier.SupplierDBModels
{
    [Table("CE_SupplierProducts")]
    public class CE_SupplierProducts
    {
        public long Id { get; set; }
        public long SupplierId { get; set; }
        public string ProductName { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string Commission { get; set; }
        public string SupplierCommsType { get; set; }

        [ForeignKey("SupplierId")]
        public virtual CE_Supplier CE_Supplier { get; set; }

    }
}