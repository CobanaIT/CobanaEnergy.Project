using System;
using System.Collections.Generic;

namespace CobanaEnergy.Project.Models.PostSales.RegInvoiceSupplierDashboard
{
    public class RegContractSelectRowViewModel
    {
        public string EId { get; set; }
        public string MPAN { get; set; }
        public string MPRN { get; set; }
        public DateTime? InputDate { get; set; }
        public string BusinessName { get; set; }
        public string PostCode { get; set; }
        public string Duration { get; set; }
        public string ContractStatus { get; set; }
        public string ContractType { get; set; }
        public string ContractNotes { get; set; }
    }

    public class RegContractSelectListingViewModel
    {
        public int UploadId { get; set; }
        public string SupplierName { get; set; }
        public List<RegContractSelectRowViewModel> Contracts { get; set; }
    }
}