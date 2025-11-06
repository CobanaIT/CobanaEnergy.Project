using System;

namespace CobanaEnergy.Project.Models.PostSales.RegInvoiceSupplierDashboard.Dto
{
    public class RegFileUploadDto
    {
        public string MPAN { get; set; }
        public string MPRN { get; set; }
        public string InputDate { get; set; }
        public string BusinessName { get; set; }
        public string SupplierName { get; set; }
        public string PostCode { get; set; }
    }
}