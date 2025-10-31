using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Accounts.InvoiceSupplierDashboard
{
    public class ContractSelectRowViewModel
    {
        public string EId { get; set; }
        public string SupplierName { get; set; }
        public string MPAN { get; set; }
        public string MPRN { get; set; }
        public DateTime? InputDate { get; set; }
        public string BusinessName { get; set; }
        public string InputEAC { get; set; }
        public string Duration { get; set; }
        public string ContractStatus { get; set; }
        public string PaymentStatus { get; set; }
        public string CED { get; set; }
    }

    public class ContractSelectListingViewModel
    {
        public int UploadId { get; set; }
        public string SupplierName { get; set; }
        public List<ContractSelectRowViewModel> Contracts { get; set; }
    }

}