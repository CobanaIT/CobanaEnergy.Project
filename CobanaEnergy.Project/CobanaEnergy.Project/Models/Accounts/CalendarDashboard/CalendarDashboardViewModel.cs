using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Accounts.CalendarDashboard
{
    public class CalendarDashboardViewModel
    {
        public List<CalendarRowViewModel> Contracts { get; set; } = new List<CalendarRowViewModel>();
    }

    public class CalendarRowViewModel
    {
        public string EId { get; set; }      
        public string InputDate { get; set; }
        public DateTime SortableDate { get; set; }  // For backend sorting
        public string PaymentStatus { get; set; }
        public string ContractStatus { get; set; }
        public string PreSalesNotes { get; set; }
        public string SupplierCobanaInvoiceNotes { get; set; }
        public string Type { get; set; }
       
    }
}