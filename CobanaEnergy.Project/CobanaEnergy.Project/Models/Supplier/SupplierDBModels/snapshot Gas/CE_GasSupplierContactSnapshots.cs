using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Supplier.SupplierDBModels.snapshot_Gas
{
    [Table("CE_GasSupplierContactSnapshots")]
    public class CE_GasSupplierContactSnapshots
    {
        public long Id { get; set; }
        public long SupplierId { get; set; }
        public long SnapshotId { get; set; }
        public string ContactName { get; set; }
        public string Role { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Notes { get; set; }

        [ForeignKey("SnapshotId")]
        public virtual CE_GasSupplierSnapshots CE_GasSupplierSnapshots { get; set; }
    }
}