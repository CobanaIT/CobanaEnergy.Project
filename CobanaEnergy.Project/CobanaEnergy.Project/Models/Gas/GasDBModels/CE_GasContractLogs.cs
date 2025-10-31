using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Gas.GasDBModels
{
    [Table("CE_GasContractLogs")]
    public class CE_GasContractLogs
    {
        [Key]
        public long Id { get; set; }

        //[ForeignKey("GasContract")]
        public string EId { get; set; }

        public string Username { get; set; }

        [Column("Date&Time")]
        public string ActionDate { get; set; }

        public string PreSalesStatusType { get; set; }
        public string Message { get; set; }

        //public virtual CE_GasContracts GasContract { get; set; }
    }
}