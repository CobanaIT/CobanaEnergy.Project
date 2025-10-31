using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Supplier.SupplierDBModels.snapshot_Gas
{
    [Table("CE_GasSupplierProductSnapshots")]
    public class CE_GasSupplierProductSnapshots
    {
        public long Id { get; set; }
        public long SnapshotId { get; set; }
        public long SupplierId { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Commission { get; set; }
        public string SupplierCommsType { get; set; }

        [ForeignKey("SnapshotId")]
        public virtual CE_GasSupplierSnapshots CE_GasSupplierSnapshots { get; set; }
    }
}