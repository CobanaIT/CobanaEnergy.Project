using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Models.Accounts.ClawbacksDashboard
{
    public class ClawbacksDashboardViewModel
    {
        public long? SupplierId { get; set; }
        public List<SelectListItem> Suppliers { get; set; } = new List<SelectListItem>();
        public List<ClawbacksRowViewModel> Contracts { get; set; } = new List<ClawbacksRowViewModel>();
    }

    public class ClawbacksRowViewModel
    {
        public string EId { get; set; }
        public string BusinessName { get; set; }
        public string MPAN { get; set; }
        public string MPRN { get; set; }
        public string InputEAC { get; set; }
        public string InputDate { get; set; }
        public string StartDate { get; set; }
        public string Duration { get; set; }
        public string PaymentStatus { get; set; }
        public string CED { get; set; }
        public string CEDCOT { get; set; }
        public string CobanaFinalReconciliation { get; set; }
    }
}