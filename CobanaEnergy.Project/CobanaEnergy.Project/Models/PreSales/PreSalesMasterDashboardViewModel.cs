using System.Collections.Generic;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Models.PreSales
{
    public class PreSalesMasterDashboardViewModel
    {
        public List<SelectListItem> Suppliers { get; set; }
        public int SubmittedCount { get; set; }
        public int RejectedCount { get; set; }
        public int PendingCount { get; set; }
    }
}
