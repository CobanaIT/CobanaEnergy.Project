using System.Collections.Generic;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Models.PostSales.RegInvoiceSupplierDashboard
{
    public class RegInvoiceSupplierUploadViewModel
    {
        public long SupplierId { get; set; }
        public IEnumerable<SelectListItem> Suppliers { get; set; }
    }
}