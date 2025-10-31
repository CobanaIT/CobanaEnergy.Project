using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Supplier.SupplierDBModels.snapshot_Gas
{
    [Table("CE_GasSupplierSnapshots")]
    public class CE_GasSupplierSnapshots
    {
        public long Id { get; set; }
        public long SupplierId { get; set; }
        public string EId { get; set; }
        public string SupplierName { get; set; }
        public string SupplierLink { get; set; }
        //public DateTime CreatedAt { get; set; }

        public virtual ICollection<CE_GasSupplierProductSnapshots> CE_GasSupplierProductSnapshots { get; set; }
        public virtual ICollection<CE_GasSupplierContactSnapshots> CE_GasSupplierContactSnapshots { get; set; }
        public virtual ICollection<CE_GasSupplierUpliftSnapshots> CE_GasSupplierUpliftSnapshots { get; set; }
    }
}