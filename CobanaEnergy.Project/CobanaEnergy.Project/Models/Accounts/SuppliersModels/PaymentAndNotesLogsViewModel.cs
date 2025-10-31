using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Accounts.SuppliersModels
{
    public class PaymentAndNotesLogsViewModel
    {
        public string EId { get; set; }
        public string PaymentStatus { get; set; }
        public string CobanaInvoiceNotes { get; set; }
        public string Username { get; set; }
        public string ContractType { get; set; }
        public string Dashboard { get; set; }
    }
}