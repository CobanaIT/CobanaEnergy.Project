using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Accounts.SuppliersModels.BGB.DBModel
{
    [Table("CE_CommissionAndReconciliation")]
    public class CE_CommissionAndReconciliation
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public string EId { get; set; }
        public string OtherAmount { get; set; }
        public string StartDate { get; set; }
        public string CED { get; set; }
        public string CED_COT { get; set; }
        public string COTLostConsumption { get; set; }
        public string CobanaDueCommission { get; set; }
        public string CobanaFinalReconciliation { get; set; }
        public string CommissionFollowUpDate { get; set; }
        public string contractType { get; set; }

        public string SupplierCobanaInvoiceNotes { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime ModifyDate { get; set; }

        // Navigation property
        public virtual ICollection<CE_CommissionMetrics> Metrics { get; set; }
    }
}