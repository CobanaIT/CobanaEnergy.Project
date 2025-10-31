using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Supplier.SupplierSnapshots
{
    public class ElectricSupplierSnapshotViewModel
    {
        public long Id { get; set; }
        public long SupplierId { get; set; }
        public string EId { get; set; }
        public string SupplierName { get; set; }
        public string Link { get; set; }

        public List<ElectricSupplierContactSnapshotViewModel> Contacts { get; set; }
        public List<ElectricSupplierProductSnapshotViewModel> Products { get; set; }
        public List<ElectricSupplierUpliftSnapshotViewModel> Uplifts { get; set; }
    }

    public class ElectricSupplierContactSnapshotViewModel
    {
        public long Id { get; set; }
        public long SupplierId { get; set; }
        public string ContactName { get; set; }
        public string Role { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Notes { get; set; }
    }

    public class ElectricSupplierProductSnapshotViewModel
    {
        public long Id { get; set; }
        public long SupplierId { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string Commission { get; set; }
        public string SupplierCommsType { get; set; }
    }

    public class ElectricSupplierUpliftSnapshotViewModel
    {
        public long Id { get; set; }
        public long SupplierId { get; set; }
        public string FuelType { get; set; }
        public string Uplift { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}