using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Models.Accounts.ProblematicsDashboard
{
    public class ProblematicsDashboardViewModel
    {
        public long? SupplierId { get; set; }
        public List<SelectListItem> Suppliers { get; set; } = new List<SelectListItem>();
        public List<ProblematicsRowViewModel> Contracts { get; set; } = new List<ProblematicsRowViewModel>();
    }

    public class ProblematicsRowViewModel
    {
        public string EId { get; set; }
        public string BusinessName { get; set; }
        public string MPAN { get; set; }
        public string MPRN { get; set; }
        public string InputEAC { get; set; }
        public string InputDate { get; set; }
        public string StartDate { get; set; }
        public string CED { get; set; }
        public string CEDCOT { get; set; }
        public string ContractStatus { get; set; }
        public string PaymentStatus { get; set; }
        public string ContractType { get; set; }
        public string Invoices { get; set; }
        public string SupplierCobanaInvoiceNotes { get; set; } = "N/A";
        public string CobanaFinalReconciliation { get; set; } = "N/A";
    }

    public class ContractUpdateModelProblematics
    {
        public string EId { get; set; }
        public string MPAN { get; set; }
        public string MPRN { get; set; }
    }
}