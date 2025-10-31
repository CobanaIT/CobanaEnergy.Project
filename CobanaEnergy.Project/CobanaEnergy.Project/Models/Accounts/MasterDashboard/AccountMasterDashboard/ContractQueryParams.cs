using CobanaEnergy.Project.Models.Common.DataTable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Accounts.MasterDashboard.AccountMasterDashboard
{
    public class ContractQueryParams :  DataTableQuery
    {
        // filters
        public string Supplier { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string Department { get; set; }

        public string BrokerageId { get; set; }
        public string StaffId { get; set; }
        public string SubBrokerageId { get; set; }
        public string ContractStatus { get; set; }
        public string PaymentStatus { get; set; }

        public string CloserId { get; set; }
        public string LeadGeneratorId { get; set; }
        public string ReferralPartnerId { get; set; }
        public string SubReferralPartnerId { get; set; }

        public string IntroducerId { get; set; }
        public string SubIntroducerId { get; set; }

        // keep these for compatibility with your controller
        public int DrawInt => Draw;
    }
}