using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.AccountsDBModel
{
    [Table("CE_Accounts")]
    public class CE_Accounts
    {
        [Key]
        public long Id { get; set; }

        public string Type { get; set; }

        public string EId { get; set; }

        //[Obsolete]
        //public long GasOrElectricId { get; set; }

        public string SortCode { get; set; }

        public string AccountNumber { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}