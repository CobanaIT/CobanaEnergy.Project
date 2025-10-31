using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Accounts.SuppliersModels.BGB
{
    public class BGBEacLogViewModel
    {
        [Required]
        public string EId { get; set; }
        public string EacYear { get; set; }
        public string EacValue { get; set; }
        public string FinalEac { get; set; }
        [Required]
        public string InvoiceNo { get; set; }
        [Required]
        public string InvoiceDate { get; set; }
        [Required]
        public string PaymentDate { get; set; }
        [Required]
        public string InvoiceAmount { get; set; }
        [Required]
        public string ContractType { get; set; }
    }
}