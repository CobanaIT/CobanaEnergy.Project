using System;

namespace CobanaEnergy.Project.Models.PreSales
{
    public class PreSalesCalendarRowViewModel
    {
        public string EId { get; set; }
        public string Agent { get; set; }
        public string MPAN { get; set; }
        public string MPRN { get; set; }
        public string PostCode { get; set; }
        public string BusinessName { get; set; }
        public string Supplier { get; set; }
        public string Type { get; set; }
        public DateTime? PreSalesFollowUpDate { get; set; }
    }
}
