using CobanaEnergy.Project.Models.Electric.ElectricDBModels.snapshot;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Supplier.SupplierDBModels.snapshot
{
    [Table("CE_ElectricSupplierProductSnapshots")]
    public class CE_ElectricSupplierProductSnapshots
    {
        public long Id { get; set; }
        public long SnapshotId { get; set; }
        public long SupplierId { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; }
        public DateTime  StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Commission { get; set; }
        public string SupplierCommsType { get; set; }

        [ForeignKey("SnapshotId")]
        public virtual CE_ElectricSupplierSnapshots CE_ElectricSupplierSnapshots { get; set; }
    }
}