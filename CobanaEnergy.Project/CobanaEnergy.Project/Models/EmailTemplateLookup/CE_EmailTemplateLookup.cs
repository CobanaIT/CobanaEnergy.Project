using CobanaEnergy.Project.Models.Accounts.MainCampaign.DBModel;
using CobanaEnergy.Project.Models.Supplier.SupplierDBModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.EmailTemplateLookup
{
    [Table("CE_EmailTemplateLookup")]
    public class CE_EmailTemplateLookup
    {
        public long Id { get; set; }
        public long SupplierId { get; set; }
        public string QueryType { get; set; }
        public string Subject { get; set; }
        public string EmailBody { get; set; }

        [ForeignKey("SupplierId")]
        public virtual CE_Supplier CE_Supplier { get; set; }
    }
}