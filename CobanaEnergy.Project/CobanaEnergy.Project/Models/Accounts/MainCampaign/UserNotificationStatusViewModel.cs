using CobanaEnergy.Project.Models.Accounts.MainCampaign.DBModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Accounts.MainCampaign
{
    public class UserNotificationStatusViewModel
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public long CampaignId { get; set; }
        public DateTime SeenAt { get; set; }
    }
}