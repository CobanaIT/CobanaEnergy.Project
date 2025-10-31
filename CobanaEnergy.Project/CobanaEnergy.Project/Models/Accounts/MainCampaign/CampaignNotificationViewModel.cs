using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Accounts.MainCampaign
{
    public class CampaignNotificationViewModel
    {
        public int Id { get; set; }
        public long CampaignId { get; set; }
        public DateTime NotifiedAt { get; set; }
        public string Message { get; set; }
    }
}