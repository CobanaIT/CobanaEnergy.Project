using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Accounts.MainCampaign.DBModel
{
    [Table("CE_CampaignNotification")]
    public class CE_CampaignNotification
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public long CampaignId { get; set; }
        [Required]
        public DateTime NotifiedAt { get; set; }
        [Required]
        public string Message { get; set; }
        // Navigation property
        [ForeignKey("CampaignId")]
        public virtual CE_Campaign Campaign { get; set; }
    }
}