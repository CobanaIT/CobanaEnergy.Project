using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Models.Accounts.AwaitingPaymentsDashboard
{
    public class AwaitingPaymentsDashboardViewModel
    {
        public long? SupplierId { get; set; }
        public List<SelectListItem> Suppliers { get; set; } = new List<SelectListItem>();
        public List<AwaitingPaymentsRowViewModel> Contracts { get; set; } = new List<AwaitingPaymentsRowViewModel>();
    }

    public class AwaitingPaymentsRowViewModel
    {
        public string EId { get; set; }
        public string BusinessName { get; set; }
        public string MPAN { get; set; }
        public string MPRN { get; set; }
        public string InputEAC { get; set; }
        public string InputDate { get; set; }
        public string StartDate { get; set; }
        public string Duration { get; set; }
        public string  ContractType { get; set; } 
        public string PaymentStatus { get; set; }
        public string InitialCommissionForecast { get; set; } = "N/A";
        public string SupplierCobanaInvoiceNotes { get; set; } = "N/A";
    }
    public class ContractUpdateModel
    {
        public string EId { get; set; }
        public string MPAN { get; set; }
        public string MPRN { get; set; }
    }


    public class EditAwaitingPaymentsViewModel
    {
        public string EId { get; set; }
        public string ContractType { get; set; }
        public string PaymentStatus { get; set; }
        public string SupplierCobanaInvoiceNotes { get; set; } = "N/A";
    }
}