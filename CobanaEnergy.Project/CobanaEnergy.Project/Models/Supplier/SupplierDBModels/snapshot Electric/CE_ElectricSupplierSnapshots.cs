using CobanaEnergy.Project.Models.Supplier.SupplierDBModels;
using CobanaEnergy.Project.Models.Supplier.SupplierDBModels.snapshot;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Electric.ElectricDBModels.snapshot
{
    [Table("CE_ElectricSupplierSnapshots")]
    public class CE_ElectricSupplierSnapshots
    {
        public long Id { get; set; }
        public long SupplierId { get; set; }
        public string EId { get; set; }
        public string SupplierName { get; set; }
        public string SupplierLink { get; set; }
        public virtual ICollection<CE_ElectricSupplierProductSnapshots> CE_ElectricSupplierProductSnapshots { get; set; }
        public virtual ICollection<CE_ElectricSupplierContactSnapshots> CE_ElectricSupplierContactSnapshots { get; set; }
        public virtual ICollection<CE_ElectricSupplierUpliftSnapshots> CE_ElectricSupplierUpliftSnapshots { get; set; }
    }
}