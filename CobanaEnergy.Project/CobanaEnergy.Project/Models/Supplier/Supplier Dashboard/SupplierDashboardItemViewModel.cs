using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Supplier.Supplier_Dashboard
{
    public class SupplierDashboardItemViewModel
    {
        public long SupplierId { get; set; }
        public string SupplierName { get; set; }
        public string Link { get; set; }
        public string ProductName { get; set; }
        public string Commission { get; set; }
        public bool Status { get; set; }
    }
}