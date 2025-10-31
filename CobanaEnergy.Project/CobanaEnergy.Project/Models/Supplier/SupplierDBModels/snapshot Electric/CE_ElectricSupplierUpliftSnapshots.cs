using CobanaEnergy.Project.Models.Electric.ElectricDBModels.snapshot;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Supplier.SupplierDBModels.snapshot
{
    [Table("CE_ElectricSupplierUpliftSnapshots")]
    public class CE_ElectricSupplierUpliftSnapshots
    {
        public long Id { get; set; }
        public long SupplierId { get; set; }
        public long SnapshotId { get; set; }
        public string FuelType { get; set; }
        public string Uplift { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        [ForeignKey("SnapshotId")]
        public virtual CE_ElectricSupplierSnapshots CE_ElectricSupplierSnapshots { get; set; }
    }
}