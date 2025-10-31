using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Accounts
{
    [Table("CE_ContractStatuses")]
    public class CE_ContractStatuses
    {
        public long Id { get; set; }
        public string EId { get; set; }
        public string Type { get; set; } 
        public string ContractStatus { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime ModifyDate { get; set; }
        public DateTime? PostSalesCreationDate { get; set; }
    }
}