using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Accounts.SuppliersModels.BGB
{
    public class UpdateContractViewModel
    {
        public string EId { get; set; }
        public bool HasElectricDetails { get; set; }
        public bool HasGasDetails { get; set; }

        public string UpliftElectric { get; set; }
        public string SupplierCommsTypeElectric { get; set; }
        public string DurationElectric { get; set; }
        public string CommissionElectric { get; set; }
        public long ElectricSnapshotId { get; set; }

        public string UpliftGas { get; set; }
        public string SupplierCommsTypeGas { get; set; }
        public string DurationGas { get; set; }
        public string CommissionGas { get; set; }
        public long GasSnapshotId { get; set; }

        public string ContractNotes { get; set; }
        public string contractStatus { get; set; }
        public string paymentStatus { get; set; }
        public string OtherAmount { get; set; }
        public string StartDate { get; set; }
        public string Ced { get; set; }
        public string CedCOT { get; set; }
        public string CotLostConsumption { get; set; }
        public string CobanaDueCommission { get; set; }
        public string CobanaFinalReconciliation { get; set; }
        public string CommissionFollowUpDate { get; set; }
        public string SupplierCobanaInvoiceNotes { get; set; }

    }
}