using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Models.InvoiceSupplierDashboard
{
    public class InvoiceSupplierUploadViewModel
    {
        public long SupplierId { get; set; }
        public IEnumerable<SelectListItem> Suppliers { get; set; }
    }
}