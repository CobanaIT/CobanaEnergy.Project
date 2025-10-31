using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Accounts.InvoiceSupplierDashboard
{
    public class ContractEditRowViewModel
    {
        public string EId { get; set; }
        public string BusinessName { get; set; }
        public string MPAN { get; set; }
        public string MPRN { get; set; }
        public string InputDate { get; set; }
        public string StartDate { get; set; }
        public string CED { get; set; }
        public string CED_COT { get; set; }
        public string Duration { get; set; }
        public string Uplift { get; set; }
        public string SupplierCommsType { get; set; }
        public string FuelType { get; set; }
        public string SupplierName { get; set; }
        public long SupplierId { get; set; }
    }

    public class ContractEditTableViewModel
    {
        public List<ContractEditRowViewModel> Contracts { get; set; }
    }

    public class SelectedContractViewModel
    {
        public string EId { get; set; }
        public string Type { get; set; }
    }

}