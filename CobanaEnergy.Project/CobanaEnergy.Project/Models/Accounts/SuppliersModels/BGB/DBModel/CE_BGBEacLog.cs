using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Accounts.SuppliersModels.BGB.DBModel
{
    [Table("CE_EacLogs")]
    public class CE_EacLogs
    {
        public long Id { get; set; }
        public string EId { get; set; }
        public string EacYear { get; set; }
        public string EacValue { get; set; }
        public string FinalEac { get; set; }
        public string InvoiceNo { get; set; }
        public string InvoiceDate { get; set; }
        public string SupplierEac { get; set; }
        public string PaymentDate { get; set; }
        public string InvoiceAmount { get; set; }
        public string ContractType { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}