using CobanaEnergy.Project.Models.Supplier.SupplierDBModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobanaEnergy.Project.Models.Accounts.MainCampaign.DBModel
{
    [Table("CE_Campaign")]
    public class CE_Campaign
    {
        public CE_Campaign()
        {
            this.CE_CampaignNotifications = new List<CE_CampaignNotification>();
            this.CE_UserNotificationStatuses = new List<CE_UserNotificationStatus>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        public Guid CId { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(250)]
        public string CampaignName { get; set; }

        [Required]
        public long SupplierId { get; set; }

        public long? ProductId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        [MaxLength(250)]
        public string SaleTarget { get; set; }

        [Required]
        [MaxLength(250)]
        public string Bonus { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property
        [ForeignKey("SupplierId")]
        public virtual CE_Supplier Supplier { get; set; }

        public virtual ICollection<CE_CampaignNotification> CE_CampaignNotifications { get; set; }
        public virtual ICollection<CE_UserNotificationStatus> CE_UserNotificationStatuses { get; set; }
    }
}
