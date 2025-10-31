using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Supplier.SupplierDBModels.snapshot_Gas
{
    [Table("CE_GasSupplierUpliftSnapshots")]
    public class CE_GasSupplierUpliftSnapshots
    {
        public long Id { get; set; }
        public long SupplierId { get; set; }
        public long SnapshotId { get; set; }
        public string FuelType { get; set; }
        public string Uplift { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        [ForeignKey("SnapshotId")]
        public virtual CE_GasSupplierSnapshots CE_GasSupplierSnapshots { get; set; }
    }
}