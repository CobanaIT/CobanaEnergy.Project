using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.PostSales.Entities
{
    [Table("CE_PostSalesLogs")]
    public class CE_PostSalesLogs
    {
        public long Id { get; set; }
        public string ContractType { get; set; }
        public string EId { get; set; }
        public string ContractStatus { get; set; }
        public string CSD { get; set; }
        public string CED { get; set; }
        public string COT { get; set; }
        public string ReAppliedDate { get; set; }
        public string QueryType { get; set; }
        public string ContractNotes { get; set; }   
        public string ObjectionDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}