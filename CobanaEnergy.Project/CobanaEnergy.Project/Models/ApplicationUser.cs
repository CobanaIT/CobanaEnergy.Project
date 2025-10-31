using CobanaEnergy.Project.Models.Accounts.MainCampaign.DBModel;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Numerics;
using System.Web;

namespace CobanaEnergy.Project.Models
{
    public class ApplicationUser : IdentityUser
    {
        public ApplicationUser()
        {
                this.CE_UserNotificationStatuses = new HashSet<CE_UserNotificationStatus>();
        }
        // Optional: Extend user profile
        public bool HasTimeRestriction { get; set; }
        public bool Enabled { get; set; }
        
        // Profile fields
        [MaxLength(50)]
        public string JobTitle { get; set; }
        
        [MaxLength(10)]
        public string ExtensionNumber { get; set; }
        
        public virtual ICollection<CE_UserNotificationStatus> CE_UserNotificationStatuses { get; set; }
    }
}