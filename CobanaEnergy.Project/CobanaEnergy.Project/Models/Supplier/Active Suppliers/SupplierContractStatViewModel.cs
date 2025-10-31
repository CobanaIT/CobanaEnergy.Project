using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Supplier.Active_Suppliers
{
    public class SupplierContractStatViewModel
    {
        public string SupplierName { get; set; }
        public string PreSalesStatus { get; set; }
        public int ElectricCount { get; set; }
        public int GasCount { get; set; }
    }
}