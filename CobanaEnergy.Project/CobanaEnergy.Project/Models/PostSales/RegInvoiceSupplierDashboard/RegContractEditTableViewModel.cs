using System.Collections.Generic;

namespace CobanaEnergy.Project.Models.PostSales.RegInvoiceSupplierDashboard
{
    public class RegContractEditRowViewModel
    {
        public string EId { get; set; }
        public string BusinessName { get; set; }
        public string MPAN { get; set; }
        public string MPRN { get; set; }
        public string InputDate { get; set; }
        public string StartDate { get; set; }
        public string CED { get; set; }
        public string CED_COT { get; set; }
        public string PostCode { get; set; }
        public string Duration { get; set; }
        public string ContractStatus { get; set; }
        public string ContractType { get; set; }
        public string ContractNotes { get; set; }
        public string SupplierName { get; set; }
        public long SupplierId { get; set; }
    }

    public class UpdateRegPostSalesFieldDto
    {
        public string EId { get; set; }
        public string InputDate { get; set; }
        public string StartDate { get; set; }
        public string CED { get; set; }
        public string COTDate { get; set; }
        public string ContractStatus { get; set; }
        public string ContractType { get; set; }
        public string Duration { get; set; }
    }

    public class RegContractEditTableViewModel
    {
        public List<RegContractEditRowViewModel> Contracts { get; set; }
    }

    public class RegSelectedContractViewModel
    {
        public string EId { get; set; }
        public string Type { get; set; }
    }
}