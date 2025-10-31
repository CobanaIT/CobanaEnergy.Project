using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Accounts.SuppliersModels.BGB.DBModel
{
    [Table("CE_PaymentAndNoteLogs")]
    public class CE_PaymentAndNoteLogs
    {
        public int Id { get; set; }
        public string EId { get; set; }
        public string PaymentStatus { get; set; }
        public string CobanaInvoiceNotes { get; set; }
        public string Username { get; set; }
        public string contracttype { get; set; }
        public string Dashboard { get; set; }
        public DateTime CreatedAt { get; set; }

    }
}