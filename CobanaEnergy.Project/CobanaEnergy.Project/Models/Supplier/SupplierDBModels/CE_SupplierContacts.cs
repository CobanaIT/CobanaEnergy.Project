using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Supplier.SupplierDBModels
{
    [Table("CE_SupplierContacts")]
    public class CE_SupplierContacts
    {
        public long Id { get; set; }
        public long SupplierId { get; set; }
        public string ContactName { get; set; }
        public string Role { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Notes { get; set; }

        [ForeignKey("SupplierId")]
        public virtual CE_Supplier CE_Supplier { get; set; }
    }
}