using System;

namespace CobanaEnergy.Project.Models.PreSales
{
    public class PreSalesMasterDashboardRowViewModel
    {
        public string EId { get; set; }
        public string Agent { get; set; }
        public string BusinessName { get; set; }
        public string CustomerName { get; set; }
        public string PostCode { get; set; }
        public string InputDate { get; set; }
        public string Duration { get; set; }
        public string Uplift { get; set; }
        public string InputEAC { get; set; }
        public string Email { get; set; }
        public string Supplier { get; set; }
        public string ContractNotes { get; set; }
        public string Type { get; set; }
        public DateTime SortableDate { get; set; }
        public string MPAN { get; set; }
        public string MPRN { get; set; }
    }
}
