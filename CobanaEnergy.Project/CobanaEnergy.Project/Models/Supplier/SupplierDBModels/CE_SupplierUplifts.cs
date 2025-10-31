using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Supplier.SupplierDBModels
{
    [Table("CE_SupplierUplifts")]
    public class CE_SupplierUplifts
    {
        public long Id { get; set; }
        public long SupplierId { get; set; }
        public string FuelType { get; set; }
        public string Uplift { get; set; } 
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        [ForeignKey("SupplierId")]
        public virtual CE_Supplier CE_Supplier { get; set; }
    }
}