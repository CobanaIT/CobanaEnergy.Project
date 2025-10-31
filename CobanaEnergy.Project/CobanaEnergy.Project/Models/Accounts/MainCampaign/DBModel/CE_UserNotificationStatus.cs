using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Accounts.MainCampaign.DBModel
{
    [Table("CE_UserNotificationStatus")]
    public class CE_UserNotificationStatus
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        [StringLength(128)]
        public string UserId { get; set; }
        [Required]
        public long CampaignId { get; set; }
        public DateTime SeenAt { get; set; }
        // Navigation properties
        [ForeignKey("CampaignId")]
        public virtual CE_Campaign Campaign { get; set; }
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } 
    }
}