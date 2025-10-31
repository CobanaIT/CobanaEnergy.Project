using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Supplier.SupplierSnapshots_Gas
{
    public class GasSupplierSnapshotViewModel
    {
        public long Id { get; set; }
        public long SupplierId { get; set; }
        public string EId { get; set; }
        public string SupplierName { get; set; }
        public string Link { get; set; }

        public List<GasSupplierContactSnapshotViewModel> Contacts { get; set; }
        public List<GasSupplierProductSnapshotViewModel> Products { get; set; }
        public List<GasSupplierUpliftSnapshotViewModel> Uplifts { get; set; }
    }

    public class GasSupplierContactSnapshotViewModel
    {
        public long Id { get; set; }
        public long SupplierId { get; set; }
        public string ContactName { get; set; }
        public string Role { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Notes { get; set; }
    }

    public class GasSupplierProductSnapshotViewModel
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

    public class GasSupplierUpliftSnapshotViewModel
    {
        public long Id { get; set; }
        public long SupplierId { get; set; }
        public string FuelType { get; set; }
        public string Uplift { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}