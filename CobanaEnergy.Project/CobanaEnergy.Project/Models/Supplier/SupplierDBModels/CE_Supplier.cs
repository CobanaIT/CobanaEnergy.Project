using CobanaEnergy.Project.Models.Accounts.MainCampaign.DBModel;
using CobanaEnergy.Project.Models.EmailTemplateLookup;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Supplier.SupplierDBModels
{
    [Table("CE_Supplier")]
    public class CE_Supplier
    {
        public CE_Supplier()
        {
                this.CE_Campaigns = new List<CE_Campaign>();    
                this.CE_EmailTemplateLookups = new List<CE_EmailTemplateLookup>();
        }
        public long Id { get; set; }
        public string Name { get; set; }
        public bool Status { get; set; }
        public string CreatedAt { get; set; }
        public string Link { get; set; }
        public virtual ICollection<CE_SupplierProducts> CE_SupplierProducts { get; set; }
        public virtual ICollection<CE_SupplierContacts> CE_SupplierContacts { get; set; }
        public virtual ICollection<CE_Campaign> CE_Campaigns { get; set; }
        public virtual ICollection<CE_EmailTemplateLookup> CE_EmailTemplateLookups { get; set; }

    }
}