using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Accounts.SuppliersModels.BGB.DBModel
{
    [Table("CE_CommissionMetrics")]
    public class CE_CommissionMetrics
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long ReconciliationId { get; set; }
        public string ContractDurationDays { get; set; }
        public string LiveDays { get; set; }
        public string PercentLiveDays { get; set; }
        public string TotalCommissionForecast { get; set; }
        public string InitialCommissionForecast { get; set; }
        public string COTLostReconciliation { get; set; }
        public string TotalAverageEAC { get; set; }
        public string contractType { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime ModifyDate { get; set; }

        // Navigation property
        [ForeignKey("ReconciliationId")]
        public virtual CE_CommissionAndReconciliation Reconciliation { get; set; }
    }
}